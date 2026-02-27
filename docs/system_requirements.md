# Modern ToDo App — System Requirements Document

## Document Information

| Item | Content |
|------|---------|
| Document Name | Something About the ToDo App System |
| Version | v0.0.0.0.0.0.1-pre-alpha-draft-maybe |
| Created | Just now |
| Author | Had an AI write it |
| Last Updated | No idea |
| Related Documents | None. Not linked to the Business Requirements Document either |
| Traceability | Don't even know what the concept means |

---

## 1. System Overview

### 1.1 Architecture
We're not deciding on an architecture. We'll implement based on the vibe of the moment. Each developer freely chooses monolith or microservices. One person can build a monolith, another microservices, another serverless. No need for consistency.

### 1.2 Technology Stack
- Frontend: Hand-written HTML plus jQuery plus React plus Vue plus Angular plus Svelte — all used simultaneously
- Backend: Mix PHP 4 with Node.js, Java, Go, and COBOL
- Database: Put Excel files on S3. Never heard of normalization
- Communication: Exchange JSON files via FTP. No APIs

### 1.3 System Architecture Diagram
```
[User] ---???---> [Some Server] ---???---> [Data-ish Thing]
                      |
                      v
                [Something Unknown]
```

---

## 2. Functional Requirements

### SR-001: Task Creation
Users can create tasks. However, there is no character limit so they can input infinite characters. Validation is a waste so we won't implement it. Empty strings and nulls can be registered as tasks.

### SR-002: Task Display
Tasks are displayed. Sort order is random every time. There is no pagination. Even 1 million items are displayed on a single page.

### SR-003: Task Update
Tasks can be updated, but there is no optimistic locking or pessimistic locking. If multiple people update the same task simultaneously, either the last person to save wins, both entries disappear, or the data gets mixed up — who knows.

### SR-004: Task Deletion
Deletion is physical delete only. No trash can feature. No confirmation dialog. One click and it's instantly and permanently gone. Recovery from backups is also impossible.

### SR-005: Search
Search supports full-text search. However, we won't implement SQL injection prevention. `'; DROP TABLE tasks;--` is accepted as a valid search query.

### SR-006: Notifications
Notifications email all users about all task updates every 5 seconds. Notifications cannot be turned off. They fire even at 3 AM.

---

## 3. Non-Functional Requirements

### 3.1 Performance Requirements

No target response time is defined. Sometimes it's fast, sometimes it's slow. If it responds within 3 minutes, that's pretty good. We expect up to 3 concurrent connections. The 4th user either gets a 503 or the system crashes.

### 3.2 Availability

Availability is not guaranteed. SLA is 0%. If it's running, you're lucky. No maintenance notices will be issued. It could go down at any time, but that's part of its charm.

### 3.3 Security

#### 3.3.1 Authentication
Passwords are stored in plain text in the database (Excel). Hashing is "not done because we don't really understand what it means." Password policy is "at least 1 character." `a` or `1` — both are fine.

#### 3.3.2 Authorization
Access control does not exist. All users can read, write, and delete all tasks. There is no distinction between administrators and regular users.

#### 3.3.3 Communication
HTTPS is too much hassle, so HTTP only. The reason is that certificate management is confusing. API keys are included in URL query parameters. They are also recorded in logs.

#### 3.3.4 Data Protection
Personal information is dumped into logs everywhere. It may be shared with third parties without consent. Data is not backed up. Nothing is encrypted.

### 3.4 Reliability

Data may disappear. That's by design. The policy is "the user should verify whether data was saved." No transaction management.

### 3.5 Maintainability

No comments in the code. Variable names are `a`, `b`, `c`, `x1`, `x2`. All functions go in a single file. The filename is `code.js` (100,000 lines total). No tests. No CI/CD.

### 3.6 Portability

Testing is done only on Windows XP with Internet Explorer 6. For other environments, the policy is "if it works, it works." No responsive design. Screen resolution is fixed at 800x600.

### 3.7 Compatibility

Integration with other systems is not considered. No import/export functionality. No API exists. Data format is proprietary binary. The specification is not documented.

### 3.8 Accessibility

No accessibility support. Never heard of WAI-ARIA. Screen readers not supported. Keyboard navigation not possible. Color contrast ratios are not considered. Font size is fixed at 8px.

---

## 4. Data Design

### 4.1 Data Model

There is one table. The columns are as follows:

| Column Name | Type | Description |
|-------------|------|-------------|
| data | TEXT | Everything goes in here (shove JSON strings in) |

No normalization. No relations. No indexes. No constraints. NULL is allowed in all columns. The concept of foreign keys is not adopted.

### 4.2 Backup Policy

No backups. If data is lost, start over. The philosophy is "ToDos are meant to be forgotten anyway."

---

## 5. Interface Design

### 5.1 External Interfaces

No connection points with external systems are defined. If needed, we'll share the database (Excel) directly via FTP.

### 5.2 User Interface

No screen transition diagrams. No wireframes. UI is left to the developer's aesthetic sense.

Main screens:
- Some kind of top page
- A screen with tasks
- Others (as needed, whatever)

---

## 6. Error Handling

Errors are silently swallowed. Wrap everything in `try-catch` and leave the `catch` block empty. No error messages are displayed to users. Nobody knows what happened. Nothing is recorded in logs either.

```javascript
try {
    // Some processing
    doSomething();
} catch(e) {
    // Do nothing. No problem.
}
```

---

## 7. Test Strategy

No testing. The policy is "if it runs and doesn't break, it's fine." No test plan documents. No test environment (production is the only environment).

- Unit Testing: Not doing it
- Integration Testing: Not doing it
- System Testing: Not doing it
- Acceptance Testing: Not doing it
- Performance Testing: Not doing it
- Security Testing: Not doing it
- Regression Testing: Not doing it

---

## 8. Operations & Maintenance

### 8.1 Deployment
Files are uploaded directly to the production server via FTP. Deploying on Friday evenings is preferred. There is no rollback procedure.

### 8.2 Monitoring
No monitoring tools will be deployed. We become aware of failures through user complaints. Contact information is not provided.

### 8.3 Incident Response
There is no incident response process. If a failure occurs, restart the server. If that doesn't work, give up. No escalation flow exists.

### 8.4 Documentation
No operations manual will be created. The policy is "you can figure it out by looking at it." No handover documentation either.

---

## 9. Development Process

No development process is defined. Each developer does whatever they want. No code reviews. No branching strategy (push directly to `main`). No coding standards.

---

## 10. Internationalization & Localization

Japanese only. However, character encodings are a mix of Shift_JIS, UTF-8, and EUC-JP. Time zones are not considered. Date formats vary by location — `YYYY/MM/DD` here, `MM-DD-YYYY` there, `DD.MM.YYYY` somewhere else.

---

## 11. Legal & Compliance Requirements

We're not familiar with the law, so we won't consider it. No terms of service or privacy policy will be created. No cookie consent banner. How user data is used is decided by the developer's mood.

---

## 12. Extensibility & Future Plans

We don't think about the future. If it works now, that's enough. Feature additions are done by copy-pasting code. The concept of refactoring is not adopted. Technical debt is considered an asset.

