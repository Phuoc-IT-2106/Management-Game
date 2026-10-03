"""Standard-library command line interface for prototype evidence."""
import argparse
import json
from pathlib import Path
import sys

from .src.state import load_calibration
from .src.simulation import Simulation, replay
from .src.verification import compare, suite, seed_range, write_json, finalize_trace_review
from .src.policies import POLICIES


def main(argv=None):
    parser=argparse.ArgumentParser(description="Deterministic headless company-management prototype")
    commands=parser.add_subparsers(dest="command",required=True)
    run=commands.add_parser("run")
    run.add_argument("--scenario",choices=list("ABCDEFGHIJKLM"),default="A")
    run.add_argument("--seed",type=int,default=1000)
    run.add_argument("--policy",choices=POLICIES)
    run.add_argument("--context",choices=("baseline","liquidity","growth"),default="baseline")
    run.add_argument("--actions",help="JSON list of checkpoint_id/choice objects")
    run.add_argument("--calibration")
    run.add_argument("--out",default="prototypes/reports/generated/run.json")
    for command in ("suite","compare"):
        sub=commands.add_parser(command)
        sub.add_argument("--seeds",default="1000:1127")
        sub.add_argument("--calibration")
        sub.add_argument("--out",default=f"prototypes/reports/generated/{command}")
    play=commands.add_parser("replay")
    play.add_argument("--input",required=True)
    review=commands.add_parser("review",help="Attach reviewed findings to exact representative traces")
    review.add_argument("--report",required=True)
    review.add_argument("--review",required=True)
    args=parser.parse_args(argv)
    try:
        if args.command=="review":
            result=finalize_trace_review(args.report,args.review)
            print(json.dumps({"recommendation":result["recommendation"],"AC-12":result["acceptance"]["AC-12"]["status"]},indent=2))
            return 0 if result["recommendation"]=="PASS PROTOTYPE GATE" else 2
        if args.command=="replay":
            result=replay(json.loads(Path(args.input).read_text(encoding="utf-8")))
            print(json.dumps(result,indent=2))
            return 0 if result["matched"] else 2
        cfg=load_calibration(args.calibration)
        if args.command=="run":
            actions=json.loads(Path(args.actions).read_text(encoding="utf-8")) if args.actions else None
            result=Simulation(args.scenario,args.seed,args.policy,cfg,args.context).finish(actions)
            write_json(Path(args.out),result)
            print(json.dumps({"metrics":result["metrics"],"state_hash":result["state_hash"],"artifact":args.out},indent=2))
        elif args.command=="compare":
            result=compare(seed_range(args.seeds),cfg)
            write_json(Path(args.out)/"comparison.json",result)
            print(json.dumps({"ac01_pass":result["ac01_pass"],"viable_policies":result["viable_policies"],"artifact":args.out},indent=2))
        else:
            result=suite(seed_range(args.seeds),cfg,args.out)
            print(json.dumps({"recommendation":result["recommendation"],
                "acceptance":{k:v["status"] for k,v in result["acceptance"].items()},"artifact":args.out},indent=2))
            return 0 if result["recommendation"]=="PASS PROTOTYPE GATE" else 2
        return 0
    except (ValueError,KeyError,TypeError,OSError) as error:
        print(f"Execution error: {error}",file=sys.stderr)
        return 1


if __name__=="__main__":
    raise SystemExit(main())
