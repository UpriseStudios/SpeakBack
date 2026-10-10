# Android Device Matrix (HUMAN STEP E)

Run on 2–3 phones (different makes and performance tiers), including your main phone. Use the dev APK from P12 unless a row says release. Paste the Dev panel Markdown snippets where indicated.

## Device

- Make / model: ______  · Android version: ______  · RAM: ______
- Granted capture rate / channels (Dev panel): ______
- APK sizes — dev: ______ release: ______

## Functional

| # | Check | Pass? | Notes |
|---|---|---|---|
| 1 | Fresh install, mic **allowed**: guided round works end to end | ☐ | |
| 2 | Fresh install, mic **denied**: explanation shown, Retry works | ☐ | |
| 3 | Denied with "don't ask again": Settings button opens app settings; allowing it there then returning works | ☐ | |
| 4 | **Release** APK, fresh install, airplane mode on: scores normally | ☐ | |
| 5 | All ten phrases play prompt → record → reveal → score | ☐ | |
| 6 | Replay, Hear what you said, Compare, Retry, Next each do the right thing | ☐ | |
| 7 | Hold the whole 8 s: stops once and processes; letting go later does nothing odd | ☐ | |
| 8 | Silent hold → "didn't catch anything", no score | ☐ | |
| 9 | Tap-to-toggle setting works | ☐ | |
| 10 | 50 consecutive rounds without a stuck state (count them) | ☐ | |

## Interruptions

| # | Check | Pass? | Notes |
|---|---|---|---|
| 11 | Lock the screen while recording, then unlock: clean ready state, mic released | ☐ | |
| 12 | Switch app during scoring, then return: no stuck "Analysing" | ☐ | |
| 13 | Incoming call (or alarm) mid-round | ☐ | |
| 14 | Connect/disconnect Bluetooth headphones mid-round | ☐ | |
| 15 | Wired headphones: prompt and reveal audible, no echo into the recording | ☐ | |

## Speech variety

| # | Check | Pass? | Notes |
|---|---|---|---|
| 16 | Quiet speech: recognised as voice and scored | ☐ | |
| 17 | Loud speech: no harsh clipping; scored | ☐ | |
| 18 | Moderate background noise (TV): still works | ☐ | |
| 19 | Input check (Dev menu): paste snippet below | ☐ | |

```
<paste Input check snippet>
```

## Performance (Dev panels)

| Metric | Target | Result |
|---|---|---|
| Capture stop → reveal start p95 | ≤ 300 ms | |
| Warm scoring p95 | ≤ 2 s | |
| Scorer cold init | record | |
| 20-minute soak: memory drift / clip count / errors | flat / 0 leaks / 0 | |
| Steady fps during play | ~60 | |

```
<paste latency + soak snippets>
```
