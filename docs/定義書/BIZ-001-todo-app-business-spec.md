# ToDo App Business Spec

> This document has no version control or revision history or anything like that. Add it as needed, if ever.

A doc summarizing the business side of the ToDo app. A web app for managing tasks, for individuals and teams alike. Create lists, add ToDos, share with others, and get realtime sync. That kind of thing.

---

## Roughly who uses it

- **Individuals** — Grocery lists, day-to-day task management, etc.
- **Team leads** — Assigning project tasks, tracking progress, etc.
- **Team members** — Checking assigned tasks, etc.
- **Management** — View-only access for progress checks. Should cut down on reporting costs

## Goals

- Make task management more efficient, boost productivity
- Make sharing tasks across teams easier
- Realtime sync so you always see the latest state
- Scale from personal use to team use, etc.

---

## Data Concepts

**List** — A container-ish thing for grouping tasks. Has a title, description, owner, and members.

**ToDo** — Individual tasks inside a list. Has a title, description, completed/not completed, due date, display order, etc.

**Membership** — A mechanism where the list owner invites others for collaboration. Three role types:

- owner → Can do everything (the creator)
- editor → Can add, edit, and delete tasks
- viewer → Can only look

---

## Main Flows

**Registration & Login**

Users register with email/password/display name → Login issues a JWT → Logout kills the session for that device only. Other devices are unaffected.

**Task Management**

Create a list → Add ToDos → Check them off when done → Edit or delete as needed. Standard CRUD stuff.

**Sharing**

The list owner searches for users and adds them as editor or viewer. Added members see the shared list in their own list view.

**Realtime Sync**

User A performs an action → Saved to DB and an Outbox event is recorded simultaneously → pg_notify triggers Functions detection → User B's UI updates automatically via SignalR. No manual reload needed.

---

## Permissions

List operations:

| Operation | owner | editor | viewer | non-member |
|-----------|-------|--------|--------|------------|
| View list/details | ○ | ○ | ○ | × |
| Create | ○ | - | - | - |
| Edit | ○ | ○ | × | × |
| Delete | ○ | × | × | × |
| Manage members | ○ | × | × | × |

ToDo operations:

| Operation | owner | editor | viewer | non-member |
|-----------|-------|--------|--------|------------|
| View | ○ | ○ | ○ | × |
| Create/Edit/Delete | ○ | ○ | × | × |
| Toggle completion | ○ | ○ | × | × |

---

## Use Cases

**Grocery list** — An individual user creates a list called "This week's groceries", adds milk, bread, etc., and checks them off at the store.

**Team sprint management** — A team lead creates a Sprint list, adds 3 members as editors, adds a manager as viewer. Everyone adds/updates tasks, and the manager views the list to grasp progress.

**Multi-device usage** — Add a ToDo on PC and it shows up on your phone in realtime. Vice versa. No reload necessary.

**Member management** — When a project wraps up, the owner removes unnecessary members. The list disappears from those members' list views. The data itself is unaffected.

---

## Business Rules, roughly

- Email is unique system-wide. Duplicate registration is not allowed
- Whoever created the list stays the owner forever. No transfers
- The owner cannot remove themselves from the member list
- If position isn't specified when creating a ToDo, it's auto-placed at the end of the list
- Deleting a list wipes all ToDos, member info, etc. inside it (cascade delete)
- Logging out on one device doesn't affect other devices
- Viewers can only look. All editing operations are rejected
- You can only invite as editor or viewer. Adding another owner is not possible

---

## Screens

| Screen | Path | What it does |
|--------|------|-------------|
| Login | `/login` | Log in with email/password |
| Register | `/register` | Create an account |
| List overview | `/lists` | Shows your lists and shared lists |
| List detail | `/lists/:id` | ToDo management, member management, etc. |

Screen flow is like `Login ↔ Register → List overview → List detail`. If not logged in, you get redirected to `/login`.

---

## KPIs or whatever

- Registration completion rate: Aiming for 80%+
- DAU: Hoping for roughly 10% month-over-month growth
- List sharing rate: Would be nice if about 30% of active users use sharing
- ToDo completion rate: 60%+
- Session duration: 5 minutes or more

Measurement methods to be decided as appropriate.

---

## Things we might do later

- Tag/label feature (higher priority, requirements already defined)
- Notifications, recurring tasks (medium priority)
- Subtasks, file attachments, dashboard (lower)
- Mobile app (someday)
- Other stuff, responding to user requests as appropriate
