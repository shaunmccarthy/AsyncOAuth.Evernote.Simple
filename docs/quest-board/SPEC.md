# Quest Board — Product & Technical Spec

> Status: **Draft v0.1** · Last updated: 2026-10-01
> Scope: Alexa (Echo Show family, incl. Echo Show 21) + parent web admin.
> Future: iPhone and Apple Watch clients on the same backend.

---

## 1. Overview

Quest Board is a fantasy-themed daily routine app for kids. Parents define recurring
**Quests** (groups of tasks such as "Before School" every weekday morning), assign them to
one or more kids, and kids complete the **Objectives** in each quest by tapping the Echo
Show screen or by talking to Alexa. Finishing a quest triggers a celebration screen with
fireworks and a quote/fact.

### Goals
- Kids can see, complete and undo their tasks by **touch or voice**.
- Works on any Echo Show, and shines as a **family board on the Echo Show 21**.
- Multiple kids, easy switching, a default kid per device.
- Parents manage everything from a **web admin** (phone or laptop).
- **Fast iteration**: everything runs and is testable in a desktop browser.
- Architecture that can later serve **iPhone / Apple Watch** without rework.

### Non-goals (v1)
- Publishing to the Alexa Skills Store (private dev skill for one family for now — but avoid
  decisions that would block publishing later).
- Rewards/points economy, parent approval of ticks, proactive reminders/notifications.
- Holiday / school-calendar awareness.
- Offline mode on Alexa.
- Photo avatars or uploaded media.

---

## 2. Glossary (themed vocabulary)

UI strings use themed vocabulary; voice accepts both themed and plain words. All UI strings
live in one strings file so names can be changed easily.

| Concept            | Themed term (UI)   | Notes |
|--------------------|--------------------|-------|
| Task group         | **Quest**          | e.g. "Before School", "Saturday Chores" |
| Task               | **Objective**      | e.g. "Brush teeth" |
| Kid                | **Adventurer**     | Has an avatar + colour |
| Group completed    | **Quest Complete!**| Triggers celebration |
| All quests today   | **Legendary Day!** | Bigger celebration |
| Streak             | **Victory Streak** | Consecutive fully-completed days |
| Missed             | **Missed**         | Deliberately neutral, not "failed" |
| Quote / fact       | **Lore**           | Placeholder content for now |
| Time window        | **Time of day**    | Morning, Afternoon, … |

---

## 3. Users & devices

| Actor | Where | Can do |
|-------|-------|--------|
| Kid (Adventurer) | Echo Show touch + voice | View quests, complete/undo objectives (same day), switch kid |
| Parent | Web admin; Echo with parent PIN | Everything below + manage kids/quests/devices, see history, undo any day |

| Device | Default layout |
|--------|----------------|
| Echo Show 5 / 8 / 10 | Single-kid view, avatar strip to switch |
| Echo Show 15 / 21 | Family board (all kids side by side), tap a kid to focus |
| Desktop browser | Same web app; dev simulator for any device size |

Initial family: 3 kids, a handful of Echo devices including one Echo Show 21.

---

## 4. Functional requirements

### 4.1 Scheduling

- **FR-S1** A family has named **time windows** with editable start/end times.
  Defaults: Morning 06:00–09:00, Afternoon 12:00–17:00, Evening 17:00–20:00,
  Bedtime 19:00–21:00.
- **FR-S2** A quest has: name, icon (emoji), **days of week** (any subset of Mon–Sun), a
  time window, and an optional **per-quest start/end override**.
- **FR-S3** A quest has an ordered list of objectives (name, emoji icon, order, optional
  spoken aliases e.g. "teeth" → "Brush teeth").
- **FR-S4** A quest is assigned to one or more kids. **Each kid completes it independently.**
  Per-kid variations are done by creating separate quests.
- **FR-S5** All dates/times use the **family timezone**. The day rolls over at **midnight**.
- **FR-S6** Quest status for a kid on a given day is *derived* (never stored):

  | Status | Rule |
  |--------|------|
  | Not today | Day of week not in schedule → not shown |
  | Upcoming | now < start |
  | Active | start ≤ now < end |
  | Complete | every objective completed today (at any time) |
  | Missed | now ≥ end and not complete |

  Missed quests don't carry over; they simply reappear on the next scheduled day.
- **FR-S7** Editing a quest takes effect immediately. Completions are keyed by objective +
  date, so renaming keeps progress; removing an objective removes it from today's count.
  Quests/objectives are **archived, not deleted**, so history stays intact.

### 4.2 Kid board (display)

- **FR-B1** The current (active) quest is shown prominently with large tappable objective
  cards (emoji + name; minimum 64px touch targets; readable by pre-readers via icons).
- **FR-B2** Later quests today are shown smaller beneath ("Coming up: Evening Quest at 5pm").
  Missed and completed quests for today are shown collapsed with their status.
- **FR-B3** Progress indicator per quest (e.g. 3/5 with a progress bar themed as a
  quest map/path).
- **FR-B4** Kid header: avatar, name, colour theme, Victory Streak.
- **FR-B5** **Echo Show 15/21 family board**: one column per kid (optimised for 2–5 kids,
  supports up to 8 with scrolling), each showing that kid's active quest. Tapping a kid's
  avatar opens their focus view.
- **FR-B6** **Smaller Echo Shows**: single-kid view with an avatar strip to switch kids.
- **FR-B7** A switched kid (or focus view) **reverts to the device default after 5 minutes**
  of inactivity.
- **FR-B8** The board refreshes from the server every few seconds while visible, so ticks made
  on other devices appear with a slight delay (target ≤ 5s).
- **FR-B9** Tapping a speaker icon makes Alexa read the active quest's remaining objectives
  aloud (for pre-readers).

### 4.3 Completing & undoing

- **FR-C1** Tap an objective card → marked complete (instant optimistic UI, then confirmed
  by the server). Tap again → undo.
- **FR-C2** By voice: "I brushed my teeth", "mark make bed done", "I finished packing my bag".
  Fuzzy matching against the current kid's objectives for today (active quest first, then
  other quests today). If ambiguous, Alexa asks: "Did you mean *Brush teeth* or
  *Brush hair*?"
- **FR-C3** Undo by voice: "undo brush teeth", "I didn't make my bed", "undo that" (most
  recent tick by this kid on this device).
- **FR-C4** **Free ticking**: no parent approval. "Mark everything done" is **not** supported
  (Alexa replies playfully that each objective needs to be done on its own).
- **FR-C5** Kids can tick/undo any of **today's** objectives until midnight, including a
  quest that is already Missed (it then becomes Complete). Parents can undo or add completions
  for any day from the admin.
- **FR-C6** Every completion records: kid, objective, date, time, source (touch / voice /
  admin), device.

### 4.4 Celebrations

- **FR-K1** When a tick completes a quest → **Quest Complete!** screen: fireworks animation,
  kid's avatar, quest name, and a **Lore** entry (quote or fact).
- **FR-K2** Triggers **once per quest per kid per day**. Undoing and re-completing doesn't
  re-trigger it.
- **FR-K3** When a kid's last quest of the day is complete → bigger **Legendary Day!**
  celebration (also once per day), showing the updated Victory Streak.
- **FR-K4** Sound: fanfare plus Alexa reading the lore aloud. A **mute** setting per device
  turns off sound and speech (visuals still play).
- **FR-K5** The celebration auto-dismisses after ~10s or on tap; on the family board it
  plays over the full screen and then returns to the board.
- **FR-K6** Lore content is a placeholder table for now (to be filled from a separate
  content effort). Each lore entry has a kind (quote/fact) and text, and is selected at
  random, avoiding repeats for a kid within the last N shown.

### 4.5 Streaks

- **FR-T1** Victory Streak = number of consecutive **scheduled days** on which the kid
  completed every quest scheduled for them (days with no quests are skipped, not breaking).
- **FR-T2** Today counts once complete; an incomplete today doesn't break the streak until
  the day ends.

### 4.6 Kid identity & switching

- **FR-I1** Each device has a **default kid** (set at pairing; changeable in admin or on the
  Echo with the parent PIN).
- **FR-I2** If Alexa recognises the speaker's **voice profile** and it's linked to a kid,
  voice requests act as that kid.
- **FR-I3** Linking a voice: a recognised but unlinked speaker says "I'm Emma" → the screen
  asks for the parent PIN to link that voice to Emma. Links can be removed in admin.
- **FR-I4** Otherwise a kid can say "I'm Emma" / "switch to Emma" (or tap an avatar) to act
  as that kid for the session (reverting per FR-B7).
- **FR-I5** Priority: explicit name in the utterance > recognised voice > session switch >
  device default.

### 4.7 Parent admin (web)

- **FR-A1** Sign in with email magic link (Supabase Auth). Multiple parent accounts per family
  (invite by email).
- **FR-A2** **Adventurers**: add / edit (name, avatar from a fixed emoji set of ~12 fantasy
  characters, colour) / reorder / delete. Delete removes the kid **and all their data**
  (with confirmation).
- **FR-A3** **Quests**: create / edit / duplicate / archive; set schedule (days + time window
  or custom times), objectives (add, edit, reorder, remove), and assigned kids.
- **FR-A4** **Time windows**: edit names and times.
- **FR-A5** **Devices**: pair a new Echo (enter the code shown on it), set label, default kid,
  mute; unpair.
- **FR-A6** **Settings**: family name, timezone, parent PIN (4 digits) for the Echo.
- **FR-A7** **History**: a weekly grid per kid (days × quests) showing complete / missed /
  partial, with tap-through to objective-level detail. Parents can toggle completions on any
  past day.
- **FR-A8** Admin is mobile-first (parents mostly use phones).

### 4.8 Device pairing

- **FR-P1** Opening the skill on an unpaired device shows (and speaks) a 4-digit code valid
  for 10 minutes.
- **FR-P2** A parent enters the code in admin → device is linked to the family and a default
  kid is chosen. The Echo screen refreshes into the board.
- **FR-P3** Alexa account linking (OAuth) is **not** used in v1 but the design leaves room to
  add it if the skill is ever published.

### 4.9 Voice interaction model

Invocation name: **"quest board"** (dev environment: **"quest board dev"**).

| Intent | Sample utterances | Behaviour |
|--------|-------------------|-----------|
| `LaunchRequest` | "Alexa, open Quest Board" | Opens the board; speaks a short summary for the current kid ("Emma, you have 3 objectives left in Before School") |
| `WhatsLeftIntent` | "what's left", "what do I need to do", "what's left for {kid}", "what's left before {quest}" | Speaks remaining objectives (active quest, then later today) |
| `CompleteObjectiveIntent` | "I {objective}", "I did {objective}", "I finished {objective}", "mark {objective} done", "{kid} finished {objective}" | Marks complete; celebrates if it completes a quest |
| `UndoObjectiveIntent` | "undo {objective}", "I didn't {objective}", "undo that", "unmark {objective}" | Undoes |
| `SwitchKidIntent` | "I'm {kid}", "this is {kid}", "switch to {kid}" | Switches kid (and voice-link flow per FR-I3) |
| `CompleteAllIntent` | "mark everything done", "I did everything" | Polite refusal (FR-C4) |
| `AMAZON.HelpIntent`, `StopIntent`, `CancelIntent`, `FallbackIntent` | — | Standard |

- `{objective}` and `{kid}` are custom slot types filled per session with **dynamic
  entities** (the current family's kids and today's objectives + aliases), backed by
  server-side fuzzy matching.
- One-shot use is supported: "Alexa, ask Quest Board what's left", "Alexa, tell Quest Board I
  brushed my teeth".
- Responses are short and in character ("Huzzah! *Brush teeth* complete. Two objectives to go,
  brave Emma!").

---

## 5. Non-functional requirements

| ID | Area | Requirement |
|----|------|-------------|
| NFR-1 | Scale | Multi-family data model; v1 used by one family (3 kids, ~30 quests, ~6 devices). |
| NFR-2 | Sync | Changes visible on other devices within ~5s (polling acceptable). |
| NFR-3 | Latency | Touch feedback < 300ms (optimistic); voice responses within normal Alexa latency (~1–2s). API p95 < 300ms. |
| NFR-4 | Availability | Best effort, no SLA. On failure Alexa says "Quest Board is having trouble, try again soon"; the board shows a friendly retry state. |
| NFR-5 | Offline | None on Alexa. Future iOS/watch clients may cache. |
| NFR-6 | Locale | English only; one timezone per family; midnight rollover. |
| NFR-7 | Privacy | Minimal data: kid first name/nickname, emoji avatar, colour. No birthdates, photos or audio stored. Alexa person IDs stored only as opaque identifiers. "Delete kid" hard-deletes all their data. |
| NFR-8 | Security | Parent admin behind Supabase Auth. Devices use a per-device secret, never a parent session. Alexa requests are signature-verified and checked against the skill ID. Parent PIN stored hashed. Data scoped by family on every query (plus Postgres RLS as defence in depth). |
| NFR-9 | Accessibility | Designed for ages 4+: icons for every objective, large touch targets, read-aloud, high-contrast bright theme, `prefers-reduced-motion` respected. |
| NFR-10 | Cost | < $10/month; expected ~$0 on Cloudflare + Supabase free tiers. |
| NFR-11 | Dev speed | The whole kid UI and admin run in a desktop browser; voice flows testable without an Echo (§6.9). One-command local dev. |
| NFR-12 | Testing | Automated unit tests for domain logic, integration tests for API + voice handlers, automated UI tests (Playwright) incl. visual snapshots at Echo resolutions. |
| NFR-13 | Environments | Separate **dev** and **prod** (separate Supabase projects, Workers and Alexa skills) so experiments never break the morning routine. |
| NFR-14 | Portability | All clients are thin; business logic lives in one shared TypeScript package behind a versioned API, so iOS/watch need no logic reimplementation. |
| NFR-15 | Publishability | Keep a path to publishing (account linking, kid-skill policies) without building for it now. |

---

## 6. Technical architecture

### 6.1 Summary of decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Echo display | **HTML via Alexa Web API for Games** (`Alexa.Presentation.HTML`) | Same React app in browser and on Echo; Playwright-testable; easy animations. Private skill, so games-only certification rules don't apply. |
| Home-screen widget (phase 2) | **APL widget** | Echo widgets must be APL; HTML can't be a widget. Small "quest progress" widget that opens the HTML app. |
| Hosting | **Cloudflare Workers** (single Worker serving static assets + API + Alexa endpoint) | Existing account, one deploy, no cold starts, free tier. |
| Database & auth | **Supabase** (Postgres + Auth) | Managed Postgres, magic-link auth, local dev via CLI, Swift SDK for later. |
| Language | **TypeScript** everywhere; pnpm workspaces | One language for UI, API, skill and logic. |
| UI | React + Vite; CSS animations + a canvas fireworks lib (e.g. `fireworks-js` / `canvas-confetti`) | Fast iteration. |
| Device linking | Pairing code | No OAuth/account-linking setup. |
| Sync | Polling (~3–5s) | Simple; Supabase Realtime later if it works on Echo. |
| Avatars | Emoji placeholders | Real art later. |

### 6.2 Component diagram

```
      Echo Show (any)                             Parent phone / laptop
 ┌──────────────────────────┐                  ┌───────────────────────┐
 │ Alexa voice ──────────┐  │                  │ Web admin (/admin)    │
 │ HTML board (/board) ◄─┼──┼─ HTML.Start ─┐   │ React SPA             │
 │  - Alexa JS bridge    │  │              │   └──────────┬────────────┘
 └──────┬────────────────┼──┘              │              │ Supabase JWT
        │ fetch /api     │ Alexa requests  │              │
        │ (device token) ▼                 │              ▼
 ┌──────┴───────────────────────────────────────────────────────────────┐
 │ Cloudflare Worker: quest-board[-dev]                                 │
 │   /            static assets (web app: /board, /admin, /dev)         │
 │   /api/*       REST API (board, completions, admin CRUD)             │
 │   /alexa       skill endpoint (signature-verified)                   │
 │   uses packages/core (schedule, status, streaks, matching, lore)     │
 └───────────────────────────────┬──────────────────────────────────────┘
                                 │ supabase-js (service role, server-side only)
                         ┌───────▼─────────┐
                         │ Supabase        │
                         │ Postgres + Auth │
                         └─────────────────┘
```

### 6.3 Key flows

**Launch on Echo**
1. "Alexa, open Quest Board" → `/alexa` `LaunchRequest`.
2. Worker looks up the device by `context.System.device.deviceId`.
   - Unpaired → create pairing code, respond with `HTML.Start` of `/board?pair` + speech.
   - Paired → mint a short-lived **device token** (JWT signed by the Worker, scoped to
     family + device), respond with `HTML.Start` of `/board` passing the token in `data`,
     plus dynamic entities (kids, today's objectives) and a spoken summary.
3. Board page initialises the Alexa JS client, reads the token, polls `/api/board`.

**Touch tick**
1. Page optimistically marks the card, `POST /api/completions`.
2. Response includes updated board + any celebration (`{kind, lore}`).
3. Page plays celebration; if not muted, sends `alexa.skill.sendMessage({type:'celebrate'})`
   so the skill can respond with spoken lore *(verify in spike)*.

**Voice tick**
1. `CompleteObjectiveIntent` → resolve kid (FR-I5) → match objective → write completion.
2. Response: speech + `Alexa.Presentation.HTML.HandleMessage` telling the page to refresh
   and play any celebration.

### 6.4 Domain logic (`packages/core`)

Pure, framework-free TypeScript functions with an injected clock; the single source of truth
for all clients.

- `scheduleFor(quest, date, tz)` → `{start, end} | null`
- `questStatus(quest, objectives, completions, now, tz)` → `upcoming|active|complete|missed`
- `buildBoard(family, kid, now)` → the **Board DTO** (the API contract for every client)
- `celebrationsTriggered(before, after)` → which celebrations a tick unlocks
- `streak(kid, history, today)`
- `matchObjective(utterance, candidates)` → ranked matches with confidence (normalisation,
  aliases, token overlap, edit distance)
- `pickLore(kid, recent)`

### 6.5 Data model (Postgres)

```sql
families        (id uuid pk, name, timezone text, parent_pin_hash, created_at)
parents         (user_id uuid pk -> auth.users, family_id fk, display_name)
kids            (id, family_id, name, avatar text, color text, sort_order,
                 created_at)                         -- hard delete cascades
kid_voice_links (kid_id fk, alexa_person_id text unique)
time_windows    (id, family_id, name, start_time time, end_time time, sort_order)
quests          (id, family_id, name, icon, days_of_week smallint[],   -- 0=Sun..6=Sat
                 time_window_id fk null, start_time time null, end_time time null,
                 sort_order, archived_at null)
quest_kids      (quest_id fk, kid_id fk, pk(quest_id, kid_id))
objectives      (id, quest_id fk, name, icon, aliases text[], sort_order, archived_at null)
completions     (id, kid_id fk, objective_id fk, local_date date, completed_at timestamptz,
                 undone_at timestamptz null, source text, device_id fk null)
                 -- unique (kid_id, objective_id, local_date) where undone_at is null
celebrations    (id, kid_id, quest_id null, kind text, local_date, lore_id, created_at)
                 -- unique (kid_id, coalesce(quest_id), kind, local_date) → FR-K2
devices         (id, family_id, alexa_device_id unique, label, default_kid_id null,
                 muted bool, paired_at, last_seen_at)
pairing_codes   (code, alexa_device_id, expires_at)
lore            (id, kind text, text, active bool)   -- placeholder seed rows
```

Status, missed and streaks are **derived** at read time, so no cron jobs. Undo sets
`undone_at` (keeps an audit trail and makes "undo that" easy).

### 6.6 API (v1)

All responses JSON; the Board DTO is versioned (`"v": 1`) because iOS/watch will consume it.

| Method & path | Auth | Purpose |
|---------------|------|---------|
| `GET /api/board?kid=` | device / parent | One kid's board for today |
| `GET /api/board/family` | device / parent | All kids (family board) |
| `POST /api/completions` `{kidId, objectiveId, date?}` | device / parent | Complete; returns board + celebration |
| `DELETE /api/completions` `{kidId, objectiveId, date?}` | device / parent | Undo (`date` ≠ today requires parent) |
| `POST /api/devices/pair` `{code, defaultKidId}` | parent | Pair device |
| `GET/POST/PATCH/DELETE /api/admin/{kids,quests,objectives,time-windows,devices,settings}` | parent | Management |
| `GET /api/admin/history?kid=&week=` | parent | Weekly grid |
| `POST /api/device/pin` `{pin, action}` | device | Unlock PIN-gated actions on the Echo |
| `POST /alexa` | Alexa signature | Skill endpoint |

### 6.7 Alexa skill details

- **Interfaces**: `Alexa.Presentation.HTML` (board), later `Alexa.DataStore` +
  `Alexa.DataStore.PackageManager` (widget).
- **Handlers**: `ask-sdk-core` running in the Worker (`nodejs_compat`). Request verification
  (cert chain URL, signature, timestamp ±150s, skill ID) implemented with WebCrypto, since the
  Node-only `ask-sdk-express-adapter` verifiers don't run on Workers.
- **Session**: the HTML app keeps the skill session open; idle timeout configured via
  `HTML.Start` `configuration.timeoutInSeconds` (max to be confirmed in spike).
- **Personalisation**: request `person.personId` (requires enabling personalisation on the
  skill) → `kid_voice_links`.
- **Skill package** (manifest + interaction model) lives in the repo and is deployed with
  the ASK CLI. Both dev and prod skills stay in the **development stage** (private to the
  family's Amazon account).

### 6.8 Home-screen widget (phase 2)

APL widget package showing each kid's avatar + progress for the active quest; tapping opens
the full HTML board. The Worker pushes updates via the **Alexa DataStore API** whenever
completions change (or the widget refreshes on a schedule). Echo Show 15/21 widget support
and update frequency to be confirmed in the spike.

### 6.9 Developer experience & testing

**Local dev**: `pnpm dev` runs Supabase (local, Docker), the Worker (`wrangler dev`) and Vite.
The web app includes an **Alexa shim**: outside an Echo, a fake `Alexa` client is injected so
the board runs in a normal browser.

**Dev simulator (`/dev`, dev env only)**: shows the board in frames sized to Echo Show 8
(1280×800), 15 and 21 (1920×1080), with:
- a text box to "say" an utterance → builds a synthetic Alexa request → `/alexa` (signature
  check bypassed in dev only) → plays speech via browser TTS and forwards `HandleMessage`
  directives to the board through the shim;
- a clock override (to test morning/evening/midnight rollover);
- device/kid/voice-profile pickers.

| Layer | Tooling | Covers |
|-------|---------|--------|
| Domain unit | Vitest, fake clock, table-driven | Schedules, status, rollover, streaks, celebrations, fuzzy matching |
| API + skill integration | Vitest + local Supabase; Alexa request JSON fixtures | Endpoints, auth scoping, intent handlers → speech/directives/DB |
| UI e2e | Playwright (Chromium) at Echo viewports, via the simulator | Tick/undo, switching, revert-to-default, celebration, admin CRUD, pairing; visual snapshots with reduced motion |
| Voice e2e | ASK CLI `ask dialog --replay` against the dev skill | Real NLU on key utterances (run manually / nightly — needs ASK credentials) |
| Device | Manual checklist on real Echos | Spike items + release smoke test |

CI (GitHub Actions): lint, typecheck, unit, integration, Playwright on every PR; deploy to
dev on merge to `main`; promote to prod manually.

### 6.10 Environments & deployment

| | dev | prod |
|-|-----|------|
| Supabase | `quest-board-dev` project | `quest-board-prod` project |
| Worker | `quest-board-dev.<acct>.workers.dev` | `quest-board.<acct>.workers.dev` (or custom domain) |
| Alexa skill | "Quest Board Dev" (invocation "quest board dev") | "Quest Board" (invocation "quest board") |
| Deploy | Auto on merge | Manual (`pnpm deploy:prod`) |

Database changes go through Supabase migrations in the repo; a seed script creates a
demo family for local dev and tests.

### 6.11 Repository layout (new repo: `quest-board`)

```
quest-board/
  apps/
    web/            React + Vite: /board, /admin, /dev, Alexa shim
    worker/         Cloudflare Worker: /api, /alexa, static assets
  packages/
    core/           domain logic + Board DTO types (no deps)
    alexa/          intent handlers, request verification, dynamic entities
  skill-package/    ASK manifest + interaction model (+ APL widget later)
  supabase/         migrations, seed.sql
  tests/
    e2e/            Playwright
    voice/          ask dialog replay files
  docs/             this spec (moved from AsyncOAuth.Evernote.Simple)
```

### 6.12 Future: iPhone & Apple Watch

SwiftUI apps call the same `/api` with a parent session (iPhone) or a device token (watch,
paired like an Echo). The Board DTO is the contract; no domain logic is ported. Possible
additions: watch complications for the active quest, push reminders (needs a notification
service).

---

## 7. Delivery plan

**Phase 0 — Device spike (½–1 day)**, deliberately before building:
1. HTML `Alexa.Presentation.HTML.Start` renders on Echo Show 21, 15, 8 (resolution, perf of
   canvas fireworks).
2. Idle timeout behaviour / maximum `timeoutInSeconds`.
3. `fetch` from the Echo page to the Worker (CORS, latency); WebSocket support (for future
   Realtime).
4. `person.personId` returned for **kids' voice profiles**; whether dev-stage skills are
   reachable when an Echo or voice is in Amazon Kids mode.
5. Page → skill message → spoken response (for touch-triggered celebration speech).
6. `ask-sdk-core` + WebCrypto signature verification on Workers.
7. Widget support on the Echo Show 21 (APL widget gallery availability for dev skills).

**Phase 1 — MVP**: core logic, Supabase schema, Worker API, admin (kids, quests, windows,
devices, settings), pairing, board (single + family layouts), touch + voice tick/undo,
switching, celebrations with placeholder lore, streaks, history, dev simulator, test suite,
dev/prod environments.

**Phase 2 — Polish**: APL home-screen widget for the 21", real avatar art and sounds, lore
content, Supabase Realtime (if supported), "Legendary Day" extras.

**Phase 3 — Apple**: iPhone (parent + kid), Apple Watch (kid), complications, reminders.

---

## 8. Risks & open questions

| # | Item | Mitigation / default |
|---|------|----------------------|
| R1 | Echo Show 21 HTML support or performance | Spike first; fall back to APL for the board if needed (UI logic stays in the API). |
| R2 | Kid voice profiles not exposed to the skill / Amazon Kids mode blocks dev skills | Fall back to device default + "I'm {kid}". |
| R3 | HTML app idle timeout returns the Echo to its home screen | Accept for v1; phase 2 widget gives an always-visible view. |
| R4 | Alexa signature verification on Workers | Small WebCrypto implementation + tests; fallback is a tiny AWS Lambda just for `/alexa`. |
| R5 | Voice matching accuracy for kids' speech | Dynamic entities + aliases + confirmation prompt on low confidence. |
| Q1 | Late ticks (after a quest's window) | Default: allowed until midnight, quest becomes Complete (FR-C5). Change if you'd rather it stay Missed. |
| Q2 | Lore content | Placeholder until the separate content chat. |

---

## 9. Decision log

| Date | Decision |
|------|----------|
| 2026-10-01 | Name **Quest Board**, fantasy theme with themed vocabulary in UI; voice accepts plain words. |
| 2026-10-01 | Private dev skill for one family; don't block publishing later; minimise setup hoops. |
| 2026-10-01 | Fixed emoji avatars (no photos); bright cartoony style. |
| 2026-10-01 | Kid identity: voice profiles + device default (+ name switching). |
| 2026-10-01 | Free ticking; undo until midnight; parents can edit any day. |
| 2026-10-01 | Missed quests show as missed, no carry-over. |
| 2026-10-01 | Management via web admin; PIN on Echo for default-kid change and voice linking. |
| 2026-10-01 | HTML (Web API for Games) for the board; APL widget for the 21" home screen in phase 2. |
| 2026-10-01 | Cloudflare Workers + Supabase; TypeScript; React + Vite. |
| 2026-10-01 | Pairing codes instead of account linking. |
| 2026-10-01 | Code in a new `quest-board` repo; spec drafted here. |
