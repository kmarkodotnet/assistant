You are a senior software architect, product designer, backend/frontend lead, and AI systems engineer.

I want to build a family-level personal assistant system.

The goal is NOT to build a simple chatbot. The goal is to build a secure, extensible family information management system that can collect, store, organize, search, summarize, process, and remind us about important information from everyday life.

The system should help a family remember, find, and manage important things across many life areas: documents, emails, deadlines, school matters, health records, invoices, warranties, car/house maintenance, subscriptions, travel, family decisions, tasks, reminders, shared notes, and it must be easily extendable to other purposes.

Important principle:
The AI must not be the source of truth. The source of truth must be our own database and document storage. AI is only an intelligent layer above the data: classification, extraction, summarization, semantic search, question answering, and recommendation.

Technology stack:
- Backend: .NET 8 / ASP.NET Core Web API
- Frontend: Angular 20, and later Kotlin frontend
- Database: PostgreSQL. If vector search is useful.
- Authentication: Google login.
- AI integration: design provider abstraction so it can use Claude/OpenAI/local model later. Prefer local model like GPT:oss 20b with ollama.
- File storage: local filesystem in MVP.
- Background jobs: use Hangfire or Quartz.NET.
- Search: combine structured database search with semantic/vector search.
- Deployment: local Docker Compose for development.

Main product vision:
Build a “Family OS”:
- family knowledge base
- document archive
- task and deadline manager
- AI search assistant
- shared family notes
- reminder engine
- integrations with Gmail, Google Calendar, Drive/OneDrive

Core MVP:
1. Family members management
2. Document upload
3. Text extraction from documents
4. Manual notes
5. Tags and topics
6. AI-generated summaries
7. AI extraction of dates, deadlines, entities, tasks
8. Search page
9. Natural language question-answering over stored family data
10. Task/deadline/reminder management
11. Audit log
12. Basic role model: admin, adult, child/read-only
13. gmail integration, later facebook integration

Important life domains to support:
- Health: medical records, lab results, medications, appointments, symptoms
- School/kids: school documents, events, deadlines, permissions
- Finance: invoices, subscriptions, payment deadlines
- Home: house maintenance, repairs, appliances
- Vehicles: insurance, technical inspection, service history
- Travel: bookings, tickets, itineraries
- Legal/admin: contracts, official papers, IDs, certificates
- Communication: important email-derived notes and decisions
- Other reminders: important dates reminder, for wxample birthdates of family members
- General family knowledge: shared notes, decisions, “where is what”

Do NOT over-engineer the first version, but design it so that it can grow.

Your task now:
Before writing production code, create a professional project blueprint. The language of the document must be hungarian.

Please produce the following deliverables:

1. Product vision
- Short description
- Main user problems
- Target users
- Primary use cases
- Non-goals for MVP

2. Domain model
Design the first version of the domain model.
Include at least:
- FamilyMember
- UserAccount
- Document
- DocumentText
- DocumentSummary
- Tag
- Topic
- Task
- Deadline
- Reminder
- Source
- AuditLog
- AiProcessingJob
- EmailMessage
- Warranty
- MedicalRecord
- FinancialRecord

Also suggest future entities:
- CalendarEvent
- Asset
- SchoolRecord
- Subscription

For each entity provide:
- purpose
- fields
- relationships
- important indexes
- validation rules

3. Database schema
Create a first database schema suitable for EF Core.
Include:
- table names
- primary keys
- foreign keys
- indexes
- enum values
- created/updated timestamps
- soft delete where useful

4. Architecture
Create a clean architecture proposal:
- API layer
- Application layer
- Domain layer
- Infrastructure layer
- AI services layer
- Background jobs
- Frontend Angular + Kotlin structure

Use clear boundaries.
The backend must not depend directly on a specific AI provider.
Create interfaces such as:
- IAiProvider
- IDocumentTextExtractor
- ISemanticSearchService
- IReminderScheduler
- INotificationService
- IDocumentClassifier
- IDeadlineExtractor
- ITaskExtractor

5. AI processing pipeline
Design how a document/note/email should be processed:
- upload/input
- text extraction
- language detection
- classification
- summary
- entity extraction
- date/deadline extraction
- suggested tasks
- suggested tags/topics
- embedding generation
- save extracted metadata
- human approval where necessary

Important:
AI-suggested tasks and deadlines should not automatically become active unless configured. They should be stored as suggestions and the user can approve them.
Important:
The AI architecture can be offline, or hybrid. When user arrives home, phone detects network, and if the local AI running PC is online, phone can instruct local AI to compute the stacked tasks.

6. Search strategy
Design hybrid search:
- normal structured filters
- full-text search
- semantic/vector search
- natural language question answering

Example questions:
- “When does the car insurance expire?”
- “Find the latest lab result for my wife.”
- “What school deadlines are coming this month?”
- “Where is the warranty for the washing machine?”
- “What did we decide about the Philippines trip?”
- “Which invoices are unpaid?”
- “Show all health-related documents from 2025.”

Important:
Questions and the application language must be hungarian.

7. Reminder engine
Design reminder logic:
- one-time reminders
- recurring reminders
- relative reminders
- deadline-based reminders
- escalation if not completed
- responsible family member
- notification channels
- future email/push integration

8. Security and privacy
Design for sensitive family data.
Include:
- authentication
- authorization
- role-based access
- audit log
- encryption recommendations
- secure file storage
- AI privacy considerations
- avoiding leaking sensitive data to external AI providers
- provider abstraction so local AI can be used later

9. Angular and later Kotlin frontend structure
Create the first Angular and Kotlin app structure:
- pages
- components
- services
- models
- routing
- guards
- state management recommendation

Main screens:
- Dashboard
- Documents
- Upload document
- Document details
- AI search
- Tasks
- Deadlines
- Reminders
- Topics
- Family members
- Settings

10. API design
Propose REST endpoints for:
- auth
- family members
- documents
- notes
- tags
- topics
- tasks
- deadlines
- reminders
- AI processing
- search
- audit logs

11. MVP backlog
Create a professional backlog:
- Epics
- User Stories
- Acceptance Criteria
- Backend dev tasks
- Frontend dev tasks
- AI/infrastructure tasks
- Priority: Must / Should / Could
- Suggested implementation order

12. Claude Code implementation plan
Create a step-by-step implementation strategy for Claude Code.
Break the work into small safe phases:
Phase 1: repository and solution structure
Phase 2: database and EF Core model
Phase 3: basic API
Phase 4: Angular shell
Phase 5: document upload
Phase 6: document text extraction
Phase 7: AI abstraction
Phase 8: summaries and extraction
Phase 9: search
Phase 10: reminders
Phase 11: dashboard
Phase 12: hardening and tests

For each phase:
- goal
- files to create/change
- tests to add
- manual verification steps
- risks

13. Coding standards
Define coding rules:
- .NET naming conventions
- DTOs vs entities
- validation
- error handling
- logging
- unit tests
- integration tests
- Angular component rules
- folder structure
- no business logic in controllers
- no AI provider calls directly from controllers

14. Output format
Do not start coding yet.
First create the architecture and planning documents in Markdown files under a /docs folder:
- docs/product-vision.md
- docs/domain-model.md
- docs/database-schema.md
- docs/architecture.md
- docs/ai-pipeline.md
- docs/search-strategy.md
- docs/reminder-engine.md
- docs/security-privacy.md
- docs/frontend-structure.md
- docs/api-design.md
- docs/mvp-backlog.md
- docs/implementation-plan.md
- docs/coding-standards.md

After generating the docs, stop and wait for review.

Very important:
- Be concrete.
- Avoid vague generic architecture.
- Make pragmatic decisions.
- Prefer an MVP that can actually be implemented.
- Do not create hundreds of unnecessary abstractions.
- Keep the design extensible but not over-engineered.
- Ask questions only if absolutely blocking. Otherwise make reasonable assumptions and document them.