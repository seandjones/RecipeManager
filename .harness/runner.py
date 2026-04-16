#!/usr/bin/env python3
"""
Harness Runner - Automated executor for harness plans

Orchestrates the execute + evaluate loop for batch processing of tasks.
"""

import json
import sys
import subprocess
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Optional
import argparse


class HarnessRunner:
    def __init__(self, plan_path: Path, dry_run: bool = False):
        self.plan_path = plan_path
        self.dry_run = dry_run
        self.plan = self._load_plan()
        
    def _load_plan(self) -> Dict:
        """Load and validate plan JSON"""
        if not self.plan_path.exists():
            raise FileNotFoundError(f"Plan not found: {self.plan_path}")
        
        with open(self.plan_path, 'r') as f:
            plan = json.load(f)
        
        required_fields = ['slug', 'title', 'tasks', 'verification_command']
        for field in required_fields:
            if field not in plan:
                raise ValueError(f"Plan missing required field: {field}")
        
        return plan
    
    def _save_plan(self):
        """Save updated plan back to file"""
        if self.dry_run:
            print(f"[DRY RUN] Would save plan to {self.plan_path}")
            return
        
        with open(self.plan_path, 'w') as f:
            json.dump(self.plan, f, indent=2)
    
    def _run_command(self, command: str) -> tuple[int, str]:
        """Run shell command and return (exit_code, output)"""
        if self.dry_run:
            print(f"[DRY RUN] Would run: {command}")
            return (0, "dry run output")
        
        result = subprocess.run(
            command,
            shell=True,
            capture_output=True,
            text=True
        )
        return (result.returncode, result.stdout + result.stderr)
    
    def get_next_pending_task(self) -> Optional[Dict]:
        """Find first pending task"""
        for task in self.plan['tasks']:
            if task['status'] == 'pending':
                return task
        return None
    
    def get_task_by_id(self, task_id: int) -> Optional[Dict]:
        """Get specific task by ID"""
        for task in self.plan['tasks']:
            if task['id'] == task_id:
                return task
        return None
    
    def verify_baseline(self) -> bool:
        """Run verification command before starting"""
        print(f"\n🔍 Verifying baseline...")
        command = self.plan['verification_command']
        exit_code, output = self._run_command(command)
        
        if exit_code != 0:
            print(f"❌ Baseline verification failed!")
            print(output)
            return False
        
        print(f"✅ Baseline verified")
        return True
    
    def execute_task(self, task: Dict) -> bool:
        """
        Execute a single task following the session protocol.
        Returns True if task completes successfully.
        """
        print(f"\n{'='*60}")
        print(f"Task #{task['id']}: {task['title']}")
        print(f"{'='*60}")
        
        # Phase 1: Orient
        print("\n📖 Acceptance Criteria:")
        for i, criterion in enumerate(task['acceptance_criteria'], 1):
            print(f"  {i}. {criterion}")
        
        if self.dry_run:
            print("\n[DRY RUN] Would execute implementation phase")
            print("[DRY RUN] Would spawn evaluator subagent")
            return True
        
        # In real execution, this would:
        # 1. Mark task as in_progress
        # 2. Trigger AI agent to implement
        # 3. Run verification
        # 4. Spawn evaluator subagent
        # 5. Process evaluator feedback
        # 6. Mark complete if PASS
        
        print("\n⚠️  Automated execution not yet implemented.")
        print("This runner currently only supports dry-run and status tracking.")
        print("\nTo execute tasks, use the /harness skill interactively.")
        
        return False
    
    def run_task(self, task_id: Optional[int] = None) -> bool:
        """Run a specific task or next pending task"""
        if task_id:
            task = self.get_task_by_id(task_id)
            if not task:
                print(f"❌ Task #{task_id} not found")
                return False
        else:
            task = self.get_next_pending_task()
            if not task:
                print("✅ No pending tasks!")
                return True
        
        if task['status'] == 'complete':
            print(f"✅ Task #{task['id']} already complete")
            return True
        
        if task['status'] == 'blocked':
            print(f"🚫 Task #{task['id']} is blocked")
            return False
        
        # Verify baseline before executing
        if not self.verify_baseline():
            print("⚠️  Fix baseline before continuing")
            return False
        
        return self.execute_task(task)
    
    def run_all(self) -> bool:
        """Run all pending tasks in sequence"""
        print(f"\n🚀 Running all tasks from plan: {self.plan['title']}")
        
        while True:
            task = self.get_next_pending_task()
            if not task:
                print("\n🎉 All tasks complete!")
                return True
            
            success = self.run_task()
            if not success:
                print(f"\n❌ Stopped at task #{task['id']}")
                return False
    
    def show_status(self):
        """Display plan status"""
        print(f"\n📋 Plan: {self.plan['title']}")
        print(f"Status: {self.plan['status']}")
        print(f"Verification: {self.plan['verification_command']}")
        
        print(f"\n📊 Tasks:")
        for task in self.plan['tasks']:
            status_icon = {
                'pending': '⏳',
                'in_progress': '🔄',
                'complete': '✅',
                'blocked': '🚫'
            }.get(task['status'], '❓')
            
            print(f"  {status_icon} #{task['id']}: {task['title']} [{task['status']}]")
            if task.get('notes'):
                print(f"     Note: {task['notes']}")


def main():
    parser = argparse.ArgumentParser(
        description='Harness Runner - Execute and track plan tasks'
    )
    parser.add_argument(
        '--plan',
        type=Path,
        required=True,
        help='Path to plan JSON file'
    )
    parser.add_argument(
        '--task',
        type=int,
        help='Run specific task by ID'
    )
    parser.add_argument(
        '--loop',
        action='store_true',
        help='Run all pending tasks in sequence'
    )
    parser.add_argument(
        '--status',
        action='store_true',
        help='Show plan status and exit'
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='Show what would be done without executing'
    )
    
    args = parser.parse_args()
    
    try:
        runner = HarnessRunner(args.plan, dry_run=args.dry_run)
        
        if args.status:
            runner.show_status()
            return 0
        
        if args.loop:
            success = runner.run_all()
        else:
            success = runner.run_task(args.task)
        
        return 0 if success else 1
    
    except Exception as e:
        print(f"❌ Error: {e}", file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
