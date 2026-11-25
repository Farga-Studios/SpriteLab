# SL2 — Animations

0.1.2: row-based named clips with stable clip and sheet UUIDs and ordered cell indices. This is a transitional adapter, not the final ADR-SL-002 Frame-ID model. Grid changes and sheet removal blocked while referenced. Schema 2 writes, schema 1 reads; older applications reject schema 2. Keep a copy before saving with the new version. Next: direction tracks and migration to full domain IDs, playback, timing, onion skin. Manual checks: create from row, select across sheets, save/reopen, remove clip, blocked grid change. CI pending.
