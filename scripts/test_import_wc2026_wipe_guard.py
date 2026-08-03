#!/usr/bin/env python3
"""Phase 15 — wipe confirm guard for import_wc2026_finished_matches.py."""

from __future__ import annotations

import unittest

import import_wc2026_finished_matches as importer


class WipeConfirmGuardTests(unittest.TestCase):
    def test_refuse_without_confirm_flag(self) -> None:
        args = importer.parse_args([])
        with self.assertRaises(SystemExit) as ctx:
            importer.require_wipe_confirmation(args)
        message = str(ctx.exception)
        self.assertIn("Refusing to wipe", message)
        self.assertIn(importer.WIPE_CONFIRM_FLAG, message)
        self.assertIn("add_sf2_and_final.py", message)

    def test_accept_with_confirm_flag(self) -> None:
        args = importer.parse_args([importer.WIPE_CONFIRM_FLAG])
        self.assertTrue(args.confirm_wipe)
        importer.require_wipe_confirmation(args)  # no raise


if __name__ == "__main__":
    unittest.main()
