#!/usr/bin/env python3
"""Local SMTP sink for E2E mail screenshots (no external send)."""
from __future__ import annotations

import asyncio
import re
from datetime import datetime, timezone
from email import message_from_bytes
from pathlib import Path

from aiosmtpd.controller import Controller

MAIL_DIR = Path("/cursor/stores/self/e2e-flow/mail")
MAIL_DIR.mkdir(parents=True, exist_ok=True)


class Sink:
    async def handle_DATA(self, server, session, envelope):  # noqa: N802
        raw = envelope.content
        msg = message_from_bytes(raw + b"\r\n")
        subject = msg.get("Subject", "(no subject)")
        to = envelope.rcpt_tos[0].decode() if envelope.rcpt_tos else "?"
        safe = re.sub(r"[^\w\-@.+]", "_", subject)[:60]
        stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S")
        base = MAIL_DIR / f"{stamp}-{safe}"
        base.with_suffix(".eml").write_bytes(raw)
        html = None
        if msg.is_multipart():
            for part in msg.walk():
                if part.get_content_type() == "text/html":
                    html = part.get_payload(decode=True)
                    break
        else:
            if msg.get_content_type() == "text/html":
                html = msg.get_payload(decode=True)
        if html:
            base.with_suffix(".html").write_bytes(html)
        (MAIL_DIR / "last.txt").write_text(f"To: {to}\nSubject: {subject}\n", encoding="utf-8")
        return "250 Message accepted"


def main() -> None:
    ctrl = Controller(Sink(), hostname="127.0.0.1", port=2525)
    ctrl.start()
    print("smtp-catcher on 127.0.0.1:2525", flush=True)
    asyncio.get_event_loop().run_forever()


if __name__ == "__main__":
    main()
