"""Offline tests for elite_scale_experiment.py (synthetic numbers only, no database, no names).

Covers: every candidate transform equals the v2 scale up to R 80, is monotone and stays within 30-99; the elite gate
still caps players without elite evidence at 89; the inverse transform round-trips; the logistic fit recovers a known
slope; and the potential read-back from the research basis text.
"""
from pathlib import Path
import random
import sys
import unittest

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import elite_scale_experiment as ex
import rating_model as rm


class TransformTests(unittest.TestCase):
    def test_identical_below_the_elite_segment(self):
        for name, knots in ex.TRANSFORMS.items():
            for r in np.arange(10.0, 80.01, 0.5):
                self.assertAlmostEqual(ex.t_of(knots, r), ex.t_of(ex.UNCAPPED, r), places=9, msg=f"{name} at R {r}")

    def test_monotone_and_bounded(self):
        for name, knots in ex.TRANSFORMS.items():
            values = [ex.t_of(knots, r) for r in np.arange(0.0, 140.0, 0.25)]
            self.assertTrue(all(b >= a for a, b in zip(values, values[1:])), name)
            self.assertGreaterEqual(min(values), 30.0)
            self.assertLessEqual(max(values), 99.0)

    def test_uncapped_matches_v2_up_to_its_old_clamp(self):
        for r in np.arange(10.0, 92.01, 0.5):
            self.assertAlmostEqual(ex.t_of(ex.UNCAPPED, r), rm.banded(r), places=9)

    def test_elite_gate_kept(self):
        record = {"elite_evidence": False, "consistency_z": 1.0}
        self.assertFalse(ex.elite_ok(record))
        self.assertEqual(ex.gated(ex.TRANSFORMS["K98"], 95.0, ex.elite_ok(record)), rm.CAP_WITHOUT_ELITE_EVIDENCE)
        self.assertTrue(ex.elite_ok({"elite_evidence": True, "consistency_z": None}))
        self.assertFalse(ex.elite_ok({"elite_evidence": True, "consistency_z": -1.5}))

    def test_inverse_round_trip(self):
        for y in np.arange(31.0, 98.9, 0.7):
            self.assertAlmostEqual(ex.t_of(ex.UNCAPPED, ex.t_inverse(ex.UNCAPPED, y)), y, places=6)

    def test_logistic_fit_recovers_slope(self):
        rng = random.Random(7)
        dx = [rng.uniform(-2, 2) for _ in range(20000)]
        y = [1 if rng.random() < 1 / (1 + np.exp(-(0.1 + 1.3 * x))) else 0 for x in dx]
        a, b, se, _ = ex.fit_logistic(dx, y)
        self.assertLess(abs(b - 1.3), 4 * se)
        self.assertLess(abs(a - 0.1), 0.06)

    def test_upper_growth_read_back(self):
        basis = "development curve to age 25: +3.2 expected, +6.4 at the 80th percentile (raw points)"
        self.assertEqual(ex.upper_growth({"potential_basis": basis}), 6.4)
        self.assertIsNone(ex.upper_growth({"potential_basis": "AGE_25_OR_OVER_NO_GROWTH"}))


if __name__ == "__main__":
    unittest.main()
