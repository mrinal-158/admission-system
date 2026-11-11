# AI usage

I used Claude Code (Anthropic, Claude Opus 5.5) in the terminal and desktop app.

## What I used it for

- Talking through the design before coding: row lock vs optimistic locking vs a seat counter,
  how expiry should work, the error format. I chose the stack and the approach.
- Writing most of the code: backend, React frontend, tests, Dockerfiles and Docker Compose.
- Drafting these docs.
- Checking the running app with curl and a real browser, including stopping the API and
  restarting the stack.

## Where it got things wrong

1. **Wrong content type on 400s.** `[Produces("application/json")]` on the controllers
   overrode `application/problem+json`. An integration test caught it; I removed the
   attribute.
2. **Timestamps changed after saving.** The API returned 7 fractional digits, PostgreSQL
   stored 6, so the same hold read back differently. I spotted it in curl output. Fixed by
   truncating to microseconds in one place, with a test.
3. **`setState` inside `useEffect`** in the countdown hook. The linter caught it; the hook now
   derives the remaining time during render.
4. **A flaky UI test** that checked a button before the data had loaded.
5. **A broken alert layout** where `space-between` pulled a sentence away from its link. I saw
   it in the browser.
6. **The demo script used `xargs -I`**, which breaks on macOS. Replaced with background jobs
   and `wait`.
7. **An invented API rule.** It made class names unique and returned 409 for a repeat. The
   brief doesn't have that, and it could break automated tests that create the same class
   twice. A review pass against the brief caught it; I removed it with a migration and a test.
8. **A missing warning.** The application page only showed "server down" when nothing was
   cached, so a parent watching the countdown got no warning. Fixed, with a test.

One more check: a concurrency test can pass by luck, so I removed `FOR UPDATE` on purpose and
confirmed the overbooking tests fail without it.

## How I kept control

I checked every requirement against the running app, not just the code. Work went in small
commits on feature branches, so each change is a readable diff. Everything was built on
11 November 2026 in one session, which is why the commit times are close together.
