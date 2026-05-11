"""
Sprint Report Formatter — AI-Powered

Reads raw git log data and formats it into a structured sprint report
using the template in Docs/Reports/_TEMPLATE.md.

Usage:
    python scripts/format_sprint_report.py \
        --input /tmp/raw_log.md \
        --template Docs/Reports/_TEMPLATE.md \
        --output "Docs/Reports/YYYY-MM-DD-sprint-report.md"

Requires:
    pip install openai  (or anthropic, or google-generativeai)

Configuration:
    Set your API key as environment variable:
    - OPENAI_API_KEY for OpenAI
    - ANTHROPIC_API_KEY for Anthropic
    - GOOGLE_API_KEY for Google

This is a stub — implement the AI call when you're ready to enable
Option B in .github/workflows/sprint-report.yml.
"""

import argparse
import os
import sys


def read_file(path: str) -> str:
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def write_file(path: str, content: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)


def format_with_ai(raw_log: str, template: str) -> str:
    """
    TODO: Implement your preferred AI provider here.

    Example with OpenAI:
        from openai import OpenAI
        client = OpenAI()
        response = client.chat.completions.create(
            model="gpt-4",
            messages=[
                {"role": "system", "content": f"You are a PM report generator. Format the raw git log into the following template structure:\\n\\n{template}"},
                {"role": "user", "content": raw_log}
            ]
        )
        return response.choices[0].message.content
    """
    print("⚠️  AI formatting not yet configured. Returning raw log as-is.", file=sys.stderr)
    print("   To enable: implement format_with_ai() in this script.", file=sys.stderr)
    return raw_log


def main():
    parser = argparse.ArgumentParser(description="Format a sprint report using AI")
    parser.add_argument("--input", required=True, help="Path to raw git log file")
    parser.add_argument("--template", required=True, help="Path to report template")
    parser.add_argument("--output", required=True, help="Output path for formatted report")
    args = parser.parse_args()

    raw_log = read_file(args.input)
    template = read_file(args.template)

    formatted = format_with_ai(raw_log, template)
    write_file(args.output, formatted)

    print(f"✅ Report saved to {args.output}")


if __name__ == "__main__":
    main()
