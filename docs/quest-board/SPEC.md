# Quest Board — Product & Technical Spec

> Status: **Draft v0.3** · Last updated: 2026-10-02
> v1 scope: **Echo Show 21 family board** (+ any web browser) and the parent web admin.
> Smaller Echo Shows follow once Amazon Kids compatibility is confirmed (§7, §8).
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
- Shines as a **family board on the Echo Show 21** first; smaller Echo Shows later.
- Multiple kids, easy switching, an optional default kid per device.
- One parent manages everything from a **web admin** (phone or laptop); the data model
  allows more parents later.
- Handles real life: **sick days, holidays and travel** don't count as missed or break
  streaks.
- **Fast iteration**: everything runs and is testable in a desktop browser.
- Architecture that can later serve **iPhone / Apple Watch** without rework.

### Non-goals (v1)
- Publishing to the Alexa Skills Store (private dev skill for one family for now — but avoid
  decisions that would block publishing later).
- Rewards/points economy, parent approval of ticks, proactive reminders/notifications.
- Automatic holiday / school-calendar awareness (parents excuse days manually, §4.6).
- One-off / specific-date quests (recurring weekly schedules only).
- Parent invites / multiple parent accounts in the UI (single parent for now).
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
| Excused / paused   | **Rest day**       | Sick days, holidays, travel |
| Quote / fact       | **Lore**           | Placeholder content for now |
| Time window        | **Time of day**    | Morning, Afternoon, … |

---

## 3. Users & devices

| Actor | Where | Can do |
|-------|-------|--------|
| Kid (Adventurer) — ages 7, 10, 13 | Echo Show touch + voice | View quests, complete/undo objectives (same day), switch kid |
| Parent (one for now) | Web admin; Echo with parent PIN | Everything below + manage kids/quests/devices, excuse days, see history, undo any day |

| Device | Default layout |
|--------|----------------|
| **Echo Show 21 (v1 target)** | Family board (all kids side by side), tap a kid to focus |
| Echo Show 5 / 8 / 10 (later) | Single-kid view, avatar strip to switch. The family's small Shows use **Amazon Kids**, which may block a private skill; see R2 |
| Screenless Echo (later) | Voice only — same intents, no display |
| Any web browser / tablet (v1) | Same board at `/board` after pairing — the first thing the family uses, before the Alexa skill is ready |
| Desktop browser (dev) | Dev simulator for any device size |

Initial family: 3 kids aged **7, 10 and 13**, one parent, one Echo Show 21 plus several
small Echo Shows set up with Amazon Kids.

**Designing for 7–13**: all three can read, so objective names are the primary content with
emoji as support (not icon-only). The look is bright and playful but should read as a
*game* rather than a toddler app, so the 13-year-old isn't put off: adventure-game styling,
cooler avatar options (rogue, dragon, mage, ranger…), and no baby-talk in Alexa's
responses.

---

## 4. Functional requirements

### 4.1 Scheduling

- **FR-S1** A family has named **time windows** with editable start/end times.
  Defaults (non-overlapping): Morning 06:00–09:00, Afternoon 12:00–17:00,
  Evening 17:00–19:30, Bedtime 19:30–21:00. A window must start and end on the same day
  (no crossing midnight), since midnight is the day rollover.
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
  | Excused | an excusal or family pause covers this kid/quest/date (§4.6) and it isn't complete |
  | Upcoming | now < start |
  | Active | start ≤ now < end |
  | Complete | every objective completed today (at any time) |
  | Missed | now ≥ end and not complete |

  Missed quests don't carry over; they simply reappear on the next scheduled day.
  Several quests can be active at once (overlapping windows or custom times); they're
  ordered by end time, soonest first.
- **FR-S8** Time maths uses the family's IANA timezone (e.g. `America/New_York`) so daylight
  saving changes are handled; quest times are wall-clock local times.
- **FR-S7** Editing a quest takes effect immediately. Completions are keyed by objective +
  date, so renaming keeps progress; removing an objective removes it from today's count.
  Quests/objectives are **archived, not deleted**, so history stays intact.

### 4.2 Kid board (display)

- **FR-B1** Active quest(s) are shown prominently with large tappable objective cards
  (name + emoji; minimum 64px touch targets).
- **FR-B2** Later quests today are shown smaller beneath ("Coming up: Evening Quest at 5pm")
  and can be expanded and **ticked early**. Missed, excused and completed quests for today
  are shown collapsed with their status.
- **FR-B2a** When nothing is active: show the next quest and its start time, or a "Rest,
  adventurer — no quests left today" screen with the streak. An excused or paused day shows
  a "Rest day" screen.
- **FR-B3** Progress indicator per quest (e.g. 3/5 with a progress bar themed as a
  quest map/path).
- **FR-B4** Kid header: avatar, name, colour theme, Victory Streak.
- **FR-B5** **Echo Show 15/21 family board**: one column per kid (optimised for 2–5 kids,
  supports up to 8 with scrolling), each showing that kid's active quest. Tapping a kid's
  avatar opens their focus view.
- **FR-B6** **Smaller Echo Shows** (after v1): single-kid view with an avatar strip to
  switch kids. Built responsively in v1 (it's also the browser/phone layout) but only
  device-tested later.
- **FR-B7** A switched kid (or focus view) **reverts to the device default after 5 minutes**
  of inactivity.
- **FR-B8** The board checks for changes every ~4s while there's recent activity, slowing to
  ~30s after 5 minutes idle, so ticks made on other devices appear within ~5s when it
  matters. The check is cheap (§6.3) and only fetches the full board when something changed.
- **FR-B9** *(Later; low priority since all kids read)* A speaker icon makes Alexa read
  the remaining objectives aloud.
- **FR-B10** Emoji (avatars, objective icons) are rendered from a **bundled SVG emoji set**
  (e.g. OpenMoji or Twemoji), not the device's emoji font, so they look the same on every
  Echo, browser and future iOS client.

### 4.3 Completing & undoing

- **FR-C1** Tap an objective card → marked complete (instant optimistic UI, then confirmed
  by the server) with a **soft chime** (toggle per device) and no speech. Tap again → undo.
- **FR-C2** By voice: "I brushed my teeth", "mark make bed done", "I finished packing my bag".
  Fuzzy matching against the current kid's objectives for today (active quest first, then
  other quests today). If ambiguous, Alexa asks: "Did you mean *Brush teeth* or
  *Brush hair*?"
- **FR-C3** Undo by voice: "undo brush teeth", "I didn't make my bed", "undo that" (most
  recent tick by this kid on this device).
- **FR-C4** **Free ticking**: no parent approval. "Mark everything done" is **not** supported
  (Alexa replies playfully that each objective needs to be done on its own).
- **FR-C5** Kids can tick/undo any of **today's** objectives until midnight: early (an
  upcoming quest) or late (a Missed quest, which then becomes Complete). Parents can undo or
  add completions for any day from the admin.
- **FR-C6** Every completion records: kid, objective, date, time, source (touch / voice /
  admin), device.
- **FR-C7** Completing is **idempotent** (a tap and a voice tick at the same moment produce
  one completion) and validated: the kid must be assigned to the quest and the quest
  scheduled on that date.

### 4.4 Celebrations

- **FR-K1** When a tick completes a quest → **Quest Complete!** screen: fireworks animation,
  kid's avatar, quest name, and a **Lore** entry (quote or fact).
- **FR-K2** Triggers **once per quest per kid per day**. Undoing and re-completing doesn't
  re-trigger it.
- **FR-K3** When a kid's last quest of the day is complete → bigger **Legendary Day!**
  celebration (also once per day), showing the updated Victory Streak.
- **FR-K4** Sound is reserved for celebrations: a short fanfare plus Alexa reading the lore
  aloud. Individual ticks only get the soft chime (FR-C1). A **mute** setting per device
  turns off all sound and speech (visuals still play).
- **FR-K5** The celebration auto-dismisses after ~10s or on tap. On the **family board** a
  short full-screen firework burst (~3s) plays, then the celebration card stays in that
  kid's column so siblings can keep tapping their own objectives.
- **FR-K7** If a voice tick completes a quest while the board isn't open (e.g. a one-shot
  "Alexa, tell Quest Board I brushed my teeth" from the home screen), a screen device opens
  the board straight into the celebration; a screenless device just speaks it.
- **FR-K6** Lore content is a placeholder table for now (to be filled from a separate
  content effort). Each lore entry has a kind (quote/fact), text and an **age band**
  (7+, 10+, 13+); it's selected at random from bands at or below the kid's band, avoiding
  repeats for a kid within the last N shown.

### 4.5 Streaks

- **FR-T1** Victory Streak = number of consecutive **scheduled days** on which the kid
  completed every quest scheduled for them. Days with no quests, and excused or paused days
  or quests (§4.6), are skipped: they neither break nor extend the streak.
- **FR-T2** Today counts once complete; an incomplete today doesn't break the streak until
  the day ends.

### 4.6 Excused days & pauses

For sick days, holidays and travel.

- **FR-E1** A parent can **excuse** a kid for a date range, either for all their quests or
  for one quest (e.g. "Emma — Before School — Mon to Wed").
- **FR-E2** A parent can **pause** the whole family for a date range (e.g. a holiday). This
  works like excusing every kid for every quest.
- **FR-E3** Excused quests are hidden from the "now" view (shown collapsed as "Excused" for
  the day), never count as Missed, don't break the streak (FR-T1) and show as excused in
  history.
- **FR-E4** A kid can still tick objectives in an excused quest if they want to (it counts
  as Complete, but there's no penalty for not doing it).
- **FR-E5** Excusals can be added for today or the future, and backdated (e.g. marking
  yesterday as a sick day fixes the streak). Added and removed from the admin; a quick
  "Excuse today" action is available on each kid's admin page.

### 4.7 Kid identity & switching

- **FR-I1** Each device has an optional **default kid** (set at pairing; changeable in admin
  or on the Echo with the parent PIN). The shared 21" family board has **no default**: on
  touch the kid is obvious from the column tapped, and by voice Alexa asks "Which adventurer
  are you?" unless the name was said or the voice recognised.
- **FR-I2** If Alexa recognises the speaker's **voice profile** and it's linked to a kid,
  voice requests act as that kid. Because the kids use Amazon Kids profiles, this may not be
  available to a private skill (R2), so it's treated as a **bonus, not a dependency**:
  everything works with names alone.
- **FR-I3** Linking a voice: a recognised but unlinked speaker says "I'm Emma" → the screen
  asks for the parent PIN to link that voice to Emma. Links can be removed in admin.
- **FR-I4** Otherwise a kid can say "I'm Emma" / "switch to Emma" (or tap an avatar) to act
  as that kid for the session (reverting per FR-B7).
- **FR-I5** Priority: explicit name in the utterance > recognised voice > session switch >
  device default.

### 4.8 Parent admin (web)

- **FR-A1** Sign in with email magic link (Supabase Auth). One parent account for now; the
  data model supports several parents per family so invites can be added later without
  migration.
- **FR-A2** **Adventurers**: add / edit (name, avatar from a fixed emoji set of ~12 fantasy
  characters, colour, age band for lore) / reorder / delete. Delete removes the kid **and all their data**
  (with confirmation).
- **FR-A3** **Quests**: create / edit / duplicate / archive; set schedule (days + time window
  or custom times), objectives (add, edit, reorder, remove), and assigned kids.
- **FR-A4** **Time windows**: edit names and times.
- **FR-A5** **Devices**: pair a new Echo (enter the code shown on it), set label, default kid,
  mute; unpair.
- **FR-A6** **Settings**: family name, timezone, parent PIN (4 digits) for the Echo.
- **FR-A6a** **Excused days & pauses**: list, add and remove (FR-E1–E5).
- **FR-A7** **History**: a weekly grid per kid (days × quests) showing complete / missed /
  partial / excused, with tap-through to objective-level detail. Parents can toggle completions on any
  past day.
- **FR-A8** Admin is mobile-first (parents mostly use phones).

### 4.9 Device pairing

- **FR-P1** Opening the skill on an unpaired device shows (and speaks) a 4-digit code valid
  for 10 minutes.
- **FR-P2** A parent enters the code in admin → device is linked to the family and a default
  kid is chosen. The Echo screen refreshes into the board.
- **FR-P3** Alexa account linking (OAuth) is **not** used in v1 but the design leaves room to
  add it if the skill is ever published.
- **FR-P4** Screenless Echos speak the code. Browsers/tablets get the same flow at `/board`.
- **FR-P5** Pairing attempts are rate-limited (e.g. 5 wrong codes per 10 min per family) and
  codes are single-use.
- **FR-P6** Alexa's device ID changes if the skill is disabled and re-enabled; the device
  then shows a new pairing code and the old device entry can be removed in admin.

### 4.10 Voice interaction model

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
- While the board is open the skill session stays active, so kids just say **"Alexa, I
  brushed my teeth"** without the invocation name. The microphone isn't left open (kids
  must say the wake word), to avoid accidental triggers.
- Voice ticks need a spoken reply (the kid asked Alexa), so it's kept very short and in
  character ("Done! Two to go, Emma."). The longer flourish, fanfare and lore are saved for
  quest completion. No baby-talk: phrasing should work for a 13-year-old.

---

## 5. Non-functional requirements

| ID | Area | Requirement |
|----|------|-------------|
| NFR-1 | Scale | Multi-family data model; v1 used by one family (3 kids, 1 parent, ~30 quests, one Echo Show 21 + browsers). |
| NFR-2 | Sync | Changes visible on other devices within ~5s (polling acceptable). |
| NFR-3 | Latency | Touch feedback < 300ms (optimistic); voice responses within normal Alexa latency (~1–2s). API p95 < 300ms. |
| NFR-4 | Availability | Best effort, no SLA. On failure Alexa says "Quest Board is having trouble, try again soon"; the board shows a friendly retry state. |
| NFR-5 | Offline | None on Alexa. Future iOS/watch clients may cache. |
| NFR-6 | Locale | English only; one timezone per family; midnight rollover. |
| NFR-7 | Privacy | Minimal data: kid first name/nickname, emoji avatar, colour, age band. No birthdates, photos or audio stored. Alexa person IDs stored only as opaque identifiers. "Delete kid" hard-deletes all their data. |
| NFR-8 | Security | Parent admin behind Supabase Auth. Devices use a per-device secret, never a parent session. Alexa requests are signature-verified and checked against the skill ID. Parent PIN stored hashed. Data scoped by family on every query (plus Postgres RLS as defence in depth). |
| NFR-9 | Accessibility | Designed for ages 7–13 (readers): text-first with supporting emoji, large touch targets (kids tapping a wall-mounted 21"), high contrast, `prefers-reduced-motion` respected. |
| NFR-10 | Cost | < $10/month; expected ~$0 on Cloudflare + Supabase free tiers. Watch two limits: Workers free tier is 100k requests/day (one board polling every 4s all day ≈ 22k, hence the idle backoff in FR-B8), and Supabase free projects **pause after ~7 days without activity** (matters for dev; a scheduled CI ping keeps it awake). |
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
| Sync | Polling with a cheap revision check | Simple; Supabase Realtime later if it works on Echo. |
| Avatars | Emoji placeholders from a bundled SVG emoji set | Consistent across devices; real art later. |
| Time handling | IANA tz via `Intl` / Temporal polyfill in `packages/core` | DST-safe; no server-local time anywhere. |

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
3. If the request started a **new session** (one-shot from the home screen), there is no
   page to message: on a screen device respond with `HTML.Start` opening
   `/board?celebrate=…` instead (FR-K7); on a screenless device, speech only.

**Change detection (polling)**
- Every write bumps `families.revision` (Postgres trigger).
- The board polls `GET /api/board/family` with `If-None-Match: "<revision>:<date>"`; the
  Worker reads only the revision (one tiny query) and returns `304 Not Modified` unless
  something changed or the date rolled over. Full board builds happen only on change.

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

Data for a board is loaded in **one round trip** via a Postgres function
(`board_data(family_id, date)` returning kids, quests, objectives, today's completions and
recent history as JSON); all rules run in TypeScript on the result. This keeps Worker →
Supabase latency to one hop and keeps logic out of SQL.

### 6.5 Data model (Postgres)

```sql
families        (id uuid pk, name, timezone text, parent_pin_hash, revision bigint,
                 created_at)                         -- revision bumped by trigger on writes
parents         (user_id uuid pk -> auth.users, family_id fk, display_name)
kids            (id, family_id, name, avatar text, color text, sort_order,
                 age_band smallint,                  -- 7 | 10 | 13, for lore selection
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
devices         (id, family_id, kind text,            -- 'alexa' | 'browser' (later 'watch')
                 alexa_device_id unique null, label, default_kid_id null,
                 layout text,                         -- 'auto' | 'single' | 'family'
                 muted bool, secret_hash null,        -- browser devices: long-lived secret
                 paired_at, last_seen_at)
pairing_codes   (code, device_ref, expires_at, used_at)
pairing_attempts(family_id, attempted_at)            -- rate limiting (FR-P5)
excusals        (id, family_id, kid_id null,         -- null kid = whole family (pause)
                 quest_id null,                      -- null quest = all quests
                 start_date date, end_date date, note text null, created_at)
lore            (id, kind text, text, min_age smallint, active bool)  -- placeholder rows
```

Status (including Excused), missed and streaks are **derived** at read time, so no cron
jobs. Undo sets
`undone_at` (keeps an audit trail and makes "undo that" easy).

### 6.6 API (v1)

All responses JSON; the Board DTO is versioned (`"v": 1`) because iOS/watch will consume it.

| Method & path | Auth | Purpose |
|---------------|------|---------|
| `GET /api/board?kid=` | device / parent | One kid's board for today |
| `GET /api/board/family` | device / parent | All kids (family board); supports `If-None-Match` → 304 |
| `POST /api/devices/register` | none (rate-limited) | Browser/tablet asks for a pairing code |
| `POST /api/completions` `{kidId, objectiveId, date?}` | device / parent | Complete; returns board + celebration |
| `DELETE /api/completions` `{kidId, objectiveId, date?}` | device / parent | Undo (`date` ≠ today requires parent) |
| `POST /api/devices/pair` `{code, defaultKidId}` | parent | Pair device |
| `GET/POST/PATCH/DELETE /api/admin/{kids,quests,objectives,time-windows,devices,settings}` | parent | Management |
| `GET /api/admin/history?kid=&week=` | parent | Weekly grid |
| `GET/POST/DELETE /api/admin/excusals` | parent | Excused days & family pauses |
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
- **Device tokens**: the token passed to the page in `HTML.Start` is a Worker-signed JWT
  (family, device; ~12h expiry). The page asks the skill for a fresh one via
  `alexa.skill.sendMessage` before expiry. Browser devices instead hold a long-lived device
  secret (stored hashed server-side, revocable by unpairing).
- **Dev bypass**: the simulator's unsigned requests to `/alexa` are accepted **only** in the
  dev Worker **and** only with a `X-Dev-Secret` header; prod never accepts unsigned requests.
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
dev on merge to `main`; promote to prod manually. A scheduled workflow pings the dev
Supabase project so it isn't paused for inactivity.

**Must-have test cases** (seeded into the suite from day one): midnight rollover while the
board is open; DST change days; overlapping active quests; undo after celebration (no
re-trigger); late tick turning Missed → Complete; simultaneous touch + voice tick
(idempotency); quest edited mid-day; kid unassigned mid-day; device token expiry/refresh.

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

Built as thin vertical slices so the family can start using something early and feedback
shapes the rest.

**M0 — Device spike (½–1 day)**, run in parallel with M1. A throwaway skill + page, tested
mainly on the **Echo Show 21**, plus one small Echo Show in Amazon Kids mode for item 4:
1. HTML `Alexa.Presentation.HTML.Start` renders on the Echo Show 21 (resolution, canvas
   fireworks performance, bundled SVG emoji, audio playback/autoplay).
2. Idle timeout behaviour / maximum `timeoutInSeconds`; does the Echo screen dim or sleep?
3. `fetch` from the Echo page to the Worker (CORS, latency); WebSocket support (for future
   Realtime).
4. **Amazon Kids**: can a kid's voice/profile open a private dev skill on (a) the 21" and
   (b) a small Show in Amazon Kids mode? Is `person.personId` returned for kids' voices?
   This decides when small Shows and voice recognition can join.
5. Page → skill message → spoken response (for touch-triggered celebration speech).
6. `ask-sdk-core` + WebCrypto signature verification on Workers.
7. Widget support on the Echo Show 21 (APL widget gallery availability for dev skills).
8. Whether the Echo's built-in **Silk browser** can show `/board` as a fallback always-on
   view.

**M1 — Board in a browser (no Alexa yet)**: repo scaffold, CI, `packages/core` with tests
(schedules, statuses, excusals, streaks), Supabase schema + seed, Worker API, `/board`
(family layout first, single-kid layout responsive, touch tick/undo with chime,
celebration with placeholder lore), browser pairing via a dev-created family.
*Usable on a tablet or the 21"'s browser within days.*

**M2 — Parent admin**: auth, kids, quests, objectives, time windows, devices, settings,
excused days & pauses, history grid.

**M3 — Alexa on the Echo Show 21**: skill package, Launch/HTML board, pairing, What's Left,
Complete/Undo, Switch Kid / "which adventurer are you?", dynamic entities, one-shot flows,
dev simulator, voice replay tests, dev + prod skills.

**M4 — Delight**: streaks + Legendary Day, celebration sound + spoken lore, mute.

**M5 — Smaller Echo Shows & voice recognition** (depends on spike item 4): single-kid
layout on device, device default kids, voice-profile linking with PIN.

**Phase 2 — Polish**: APL home-screen widget for the 21", real avatar art, lore content,
Supabase Realtime (if supported), read-aloud, screenless Echos.

**Phase 3 — Apple**: iPhone (parent + kid), Apple Watch (kid), complications, reminders.

---

## 8. Risks & open questions

| # | Item | Mitigation / default |
|---|------|----------------------|
| R1 | Echo Show 21 HTML support or performance | Spike first; fall back to APL for the board if needed (UI logic stays in the API). |
| R2 | **Confirmed relevant**: the kids use Amazon Kids. Kid voice profiles may not be exposed to the skill, and Amazon Kids mode may block private dev skills on the small Shows | v1 targets the 21"; identity works by name/touch alone. If small Shows are blocked: switch them out of Amazon Kids mode, or publish as a kids skill later (NFR-15). |
| R3 | HTML app idle timeout returns the Echo to its home screen | Accept for v1; phase 2 widget gives an always-visible view; Silk browser as possible fallback. |
| R4 | Alexa signature verification on Workers | Small WebCrypto implementation + tests; fallback is a tiny AWS Lambda just for `/alexa`. |
| R5 | Voice matching accuracy for kids' speech | Dynamic entities + aliases + confirmation prompt on low confidence. |
| R6 | Device emoji fonts missing/outdated on Echo | Bundled SVG emoji set (FR-B10). |
| R7 | Free-tier limits (Workers requests, Supabase pausing) | Revision-based polling with backoff; CI keep-alive ping for dev. |
| R8 | Alexa device IDs change after skill re-enable | Re-pair flow (FR-P6). |

### Open questions (v0.3)

| # | Question | Proposed default |
|---|----------|------------------|
| Q1 | Lore content | Placeholder until the separate content chat; rows carry an age band. |
| Q2 | Is the **Echo Show 21** itself in Amazon Kids mode, or a normal (adult) family device? | Assumed normal mode; the spike confirms the skill opens there. |
| Q3 | Visual style that suits a 13-year-old as well as a 7-year-old | Bright adventure-game look; revisit once real art is chosen. |

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
| 2026-10-02 | v0.2 review: bundled SVG emoji; revision-based polling with idle backoff; one-round-trip board data; device tokens + browser devices; screenless Echo support; one-shot celebration flow; non-overlapping default windows and overlap rules; DST-safe time; idempotent ticks; pairing rate limits; milestone plan that ships a browser board before Alexa. |
| 2026-10-02 | v0.3: v1 targets the Echo Show 21 + browsers; small (Amazon Kids) Shows and voice recognition move to M5. Excused days & family pauses added. No one-off quests. Late and early ticks confirmed. Kids aged 7/10/13: text-first, not babyish, lore age bands. Single parent for now. Ticks get a soft chime only; sound reserved for celebrations. |
