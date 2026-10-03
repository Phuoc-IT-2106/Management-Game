import contextlib
import io
import json
from pathlib import Path
import shutil
import uuid
import unittest

from prototypes.__main__ import main
from prototypes.src.state import load_calibration
from prototypes.src.verification import controlled_probes, seed_range, finalize_trace_review


@contextlib.contextmanager
def workspace_temp():
    root = Path(__file__).resolve().parents[1] / "reports" / "generated" / "test-temp"
    root.mkdir(parents=True, exist_ok=True)
    assert root.resolve().is_relative_to(Path(__file__).resolve().parents[1])
    directory = root / uuid.uuid4().hex
    directory.mkdir()
    try:
        yield directory
    finally:
        assert directory.resolve().parent == root.resolve()
        shutil.rmtree(directory)


class VerificationTests(unittest.TestCase):
    def test_seed_ranges_inclusive_and_reject_inversion(self):
        self.assertEqual([0,1,2],seed_range("0:2"))
        self.assertEqual([5],seed_range("5"))
        with self.assertRaises(ValueError):
            seed_range("2:0")

    def test_preparation_choices_change_with_reachable_coordination_and_meta(self):
        result=controlled_probes(range(8),load_calibration())
        self.assertEqual(3,result["preparation_rank_reversals"])
        preferred={x["preferred"] for x in result["preparation_contexts"].values()}
        self.assertEqual({0,1,2},preferred)

    def test_debt_useful_for_timing_but_not_free_net_value(self):
        result=controlled_probes(range(8),load_calibration())["debt"]
        without,with_loan=result["bridge_over_gap"]
        self.assertFalse(without["paid_on_time"])
        self.assertTrue(with_loan["paid_on_time"])
        self.assertLess(with_loan["ending_cash"],without["ending_cash"])
        self.assertLess(result["unneeded_loan_net_liquidity_change"],0)

    def test_run_actions_replay_cli_and_error_exit_codes(self):
        with workspace_temp() as directory, contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            out=Path(directory)/"run.json"
            self.assertEqual(0,main(["run","--scenario","A","--seed","7","--out",str(out)]))
            self.assertEqual(0,main(["replay","--input",str(out)]))
            result=json.loads(out.read_text(encoding="utf-8"))
            actions=Path(directory)/"actions.json"
            actions.write_text(json.dumps(result["decisions"]),encoding="utf-8")
            second=Path(directory)/"second.json"
            self.assertEqual(0,main(["run","--scenario","A","--seed","7","--actions",str(actions),"--out",str(second)]))
            self.assertEqual(result["state_hash"],json.loads(second.read_text())["state_hash"])
            actions.write_text('[{"checkpoint_id":"bad","choice":"retain"}]',encoding="utf-8")
            self.assertEqual(1,main(["run","--actions",str(actions),"--out",str(second)]))

    def test_calibration_rejects_negative_financing(self):
        cfg=load_calibration()
        cfg["finance"]["bridge_fee"]=-1
        with workspace_temp() as directory:
            path=Path(directory)/"invalid.json"
            path.write_text(json.dumps(cfg),encoding="utf-8")
            with self.assertRaises(ValueError):
                load_calibration(str(path))

    def test_trace_review_rejects_different_calibration(self):
        with workspace_temp() as directory:
            path=Path(directory)
            (path/"verification.json").write_text(json.dumps({"simulation_version":"one","calibration_hash":"one"}),encoding="utf-8")
            (path/"review.json").write_text(json.dumps({"simulation_version":"one","calibration_hash":"two"}),encoding="utf-8")
            with self.assertRaises(ValueError):
                finalize_trace_review(path,path/"review.json")


if __name__=="__main__":
    unittest.main()
