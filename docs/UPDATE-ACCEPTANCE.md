# Windows acceptance — 0.6.8 → 0.6.9

Use a disposable Windows user or VM and a copy of a real project. Do not publish stable metadata until all pre-publication checks pass.

| Check | Expected result |
| --- | --- |
| Candidate install | Installs per user; launches; About reports expected version |
| Icon | Explorer, taskbar, title bar and installer show approved artwork |
| Bilingual UI | Spanish and English settings persist after restart |
| Project safety | Import, animate, save, reopen; original PNG unchanged |
| Baseline trust | Maintainer public key embedded; private key outside repository |
| New candidate | Installer filename and executable version agree |
| Signed update | Valid newer metadata offers installer; same/older version is not offered |
| Tampering | Altered metadata/signature or installer rejected |
| Cancel | Cancel download or pending-project confirmation; app remains usable |
| Installation | Save pending edits; old process exits; installer completes; reopen manually if needed |
| Persistence | Projects and preferences preserved |
| Portable | Manual update guidance; no automatic install |
| Offline | Failure shown without damaging application |
| Recovery | Keep previous verified installer and external project backup; automatic rollback not implemented |

Record actual outcomes and screenshots. CI passing is not a substitute for Windows acceptance.
