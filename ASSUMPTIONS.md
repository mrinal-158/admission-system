# Assumptions and decisions

Places where the brief left room, and what I picked.

## API

- **Class names can repeat.** The brief only defines `201` for `POST /classes`, so I didn't add
  a 409 for a duplicate name. A class is identified by its id.
- **Student id** is the 17-digit birth registration number in `X-Student-Id`. Anything else
  gets 400 `INVALID_STUDENT_ID`. The UI also accepts Bangla digits and converts them.
- **No login**, as the brief says. Anyone can create a class and the student id is trusted. A
  real system would need authentication for both.
- **One active application per student per class.** Applying to the other shift is allowed,
  and so is applying again after an expiry or a cancel.
- **Confirm** works only for the owner, only while the hold is HELD and not expired. Calling it
  twice returns 200 and still counts one seat, because payment retries happen.
- **Cancel** releases a HELD seat. Cancelling an expired or cancelled hold is a no-op (204).
  A confirmed admission can't be cancelled (409), since that would mean a refund.
- **Someone else's hold** returns 403, not 404: the hold exists. `GET /holds/{id}` needs no
  header, as in the brief.
- Expired and cancelled applications stay in "My applications" as history.
- Seats must be 1 to 10,000 and names at most 100 characters.
- Errors are RFC 7807 problem JSON with a stable `code` the UI maps to a message.
- Times are UTC and truncated to microseconds, so a value reads back from PostgreSQL exactly
  as the API returned it.

## Expiry

There is no background job. A seat counts as taken while the hold is HELD and
`expires_at > now`, or when it is CONFIRMED. So a seat frees itself the moment the hold runs
out, even if the API is down at that moment. The stored status is updated to EXPIRED later,
inside the locked transaction of the next application for that class.

## Concurrency

Apply, confirm and cancel each run in one transaction that starts with
`SELECT ... FROM classes WHERE id = @id FOR UPDATE`. Requests for the same class wait for each
other; different classes don't. Because the lock is in PostgreSQL, it also works with several
API instances on one database. Locks are always taken class first, then hold, so there are no
deadlocks. A partial unique index on `(class_id, student_id)` for active holds backs up the
one-per-class rule.

I also considered optimistic concurrency (lots of retries during a rush), `SERIALIZABLE`
(needs retry loops) and an atomic seat counter (fast, but needs a job to release expired
seats). The row lock is held for a few milliseconds, which is fine here.

If I remove `FOR UPDATE`, the overbooking tests fail, so they really test the lock.

"Now" comes from each API process, so instances on different machines need synced clocks.
If that was a concern, the transaction could use PostgreSQL's `now()` instead.

## Frontend

- English UI. The brief doesn't ask for Bangla.
- Polling instead of WebSockets: seat counts every 3 s, an open application every 5 s.
- The countdown uses the server's `Date` header, so a wrong phone clock can't keep "Pay"
  enabled after expiry. At zero it checks with the server.
- The student id is kept in `localStorage`, so applications survive a refresh.
- The API URL is set at container start (`API_BASE_URL`), so one image works with any backend.

## Operations

- Migrations run at start-up and retry while PostgreSQL is booting.
- PostgreSQL isn't published to the host, so it can't clash with a local install on 5432.
- .NET 10 (current LTS), since .NET 8 support ends in November 2026.
- No rate limiting. A 429 isn't in the contract, and many parents share one IP behind mobile
  carrier NAT. In production it would sit at the gateway, after real authentication.
