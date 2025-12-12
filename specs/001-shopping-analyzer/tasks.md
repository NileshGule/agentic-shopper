# Tasks: Smart Shopping Pattern Analyzer & Recommender

**Input**: Design documents from `/specs/001-shopping-analyzer/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Feature**: Multi-agent shopping analyzer using Microsoft Agent Framework, C# 12 (.NET 10), React 18, PostgreSQL, Azure AI services  
**User Stories**: 7 total (P1: 2 stories, P2: 2 stories, P3: 3 stories)  
**Agents**: 7 autonomous agents + 1 coordinator  
**Tests**: NOT explicitly requested in specification - tests excluded per mode instructions

## Format: `- [ ] [TaskID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1-US7) for user story phase tasks
- **NO [Story]**: Setup and Foundational phase tasks

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create backend solution structure with 9 projects (Coordinator, 6 agents, Core, Data) in backend/src/
- [X] T002 Initialize .NET 10 solution file and configure project references in backend/
- [X] T003 [P] Create frontend React 18 + TypeScript project with Vite in frontend/
- [X] T004 [P] Configure ESLint, Prettier, and EditorConfig for code quality
- [ ] T005 [P] Setup Docker Compose configuration in backend/deployment/docker/docker-compose.yml
- [X] T006 [P] Create .gitignore for .NET, React, and Docker artifacts
- [ ] T007 Create repository structure per plan.md (backend/, frontend/, .github/, specs/)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST complete before ANY user story implementation

**⚠️ CRITICAL**: No user story work can begin until Phase 2 is complete

- [X] T008 Setup PostgreSQL database schema in backend/src/AgenticShopper.Data/ApplicationDbContext.cs
- [X] T009 Create initial EF Core migration for all 11 tables in backend/src/AgenticShopper.Data/Migrations/
- [X] T010 [P] Implement FamilyAccount entity in backend/src/AgenticShopper.Core/Models/FamilyAccount.cs
- [X] T011 [P] Implement UserProfile entity in backend/src/AgenticShopper.Core/Models/UserProfile.cs
- [X] T012 [P] Implement Store entity in backend/src/AgenticShopper.Core/Models/Store.cs
- [X] T013 [P] Implement Category entity in backend/src/AgenticShopper.Core/Models/Category.cs
- [X] T014 Seed predefined categories (11 categories) and stores (Coles, Woolworths) in migration
- [X] T015 Configure EF Core relationships and indexes in backend/src/AgenticShopper.Data/ApplicationDbContext.cs
- [X] T016 [P] Implement ILlmProvider abstraction in backend/src/AgenticShopper.Core/Interfaces/ILlmProvider.cs
- [X] T017 [P] Implement FoundryLocalProvider in backend/src/AgenticShopper.Core/Abstractions/FoundryLocalProvider.cs
- [X] T018 [P] Implement AzureAIFoundryProvider in backend/src/AgenticShopper.Core/Abstractions/AzureAIFoundryProvider.cs
- [X] T019 Implement LLM provider factory with configuration-based selection in backend/src/AgenticShopper.Core/Abstractions/LlmProviderFactory.cs
- [X] T020 [P] Configure Microsoft Agent Framework runtime in backend/src/AgenticShopper.Coordinator/Program.cs
- [X] T021 [P] Implement AgentBase abstract class in backend/src/AgenticShopper.Core/Abstractions/AgentBase.cs
- [ ] T022 [P] Setup JWT authentication middleware in backend/src/AgenticShopper.Coordinator/Middleware/
- [X] T023 [P] Configure CORS for React frontend in backend/src/AgenticShopper.Coordinator/Program.cs
- [X] T024 [P] Setup error handling middleware in backend/src/AgenticShopper.Coordinator/Middleware/ErrorHandlingMiddleware.cs
- [X] T025 [P] Configure structured logging with Serilog in backend/src/AgenticShopper.Coordinator/Program.cs
- [X] T026 [P] Setup environment configuration management (appsettings.json) in all agent projects
- [X] T027 Create base repository interface IRepository<T> in backend/src/AgenticShopper.Core/Interfaces/IRepository.cs
- [X] T028 [P] Setup SignalR hub infrastructure in backend/src/AgenticShopper.Coordinator/Hubs/ShoppingListHub.cs
- [X] T029 [P] Configure Azure Blob Storage client in backend/src/AgenticShopper.Core/Services/BlobStorageService.cs
- [X] T030 [P] Setup Redis caching configuration in backend/src/AgenticShopper.Coordinator/Program.cs
- [ ] T031 [P] Create Dockerfile.coordinator in backend/deployment/docker/Dockerfile.coordinator
- [ ] T032 [P] Create Dockerfile.agent for agent services in backend/deployment/docker/Dockerfile.agent
- [X] T033 [P] Setup API routing structure in backend/src/AgenticShopper.Coordinator/Controllers/
- [X] T034 [P] Configure Swagger/OpenAPI documentation in backend/src/AgenticShopper.Coordinator/Program.cs
- [X] T035 Create React app structure with routing (React Router) in frontend/src/
- [X] T036 [P] Setup Axios HTTP client configuration in frontend/src/services/api/client.ts
- [X] T037 [P] Setup SignalR client connection in frontend/src/services/websocket/realtimeSync.ts
- [X] T038 [P] Create common UI components (Button, Input, Modal) in frontend/src/components/common/

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Bill Capture and Product Recognition (Priority: P1) 🎯 MVP

**Goal**: Upload receipts, extract products via OCR, allow manual review/correction

**Independent Test**: Upload grocery receipt → Verify extracted products, prices, store → Manually correct errors → Confirm data saved

### Implementation for User Story 1

- [X] T039 [P] [US1] Create Receipt entity in backend/src/AgenticShopper.Core/Models/Receipt.cs
- [X] T040 [P] [US1] Create Product entity in backend/src/AgenticShopper.Core/Models/Product.cs
- [X] T041 [P] [US1] Create Purchase entity in backend/src/AgenticShopper.Core/Models/Purchase.cs
- [X] T042 [US1] Add Receipt, Product, Purchase tables to EF Core migration (depends on T039-T041)
- [X] T043 [US1] Implement ReceiptRepository in backend/src/AgenticShopper.Data/Repositories/ReceiptRepository.cs
- [X] T044 [P] [US1] Implement ProductRepository in backend/src/AgenticShopper.Data/Repositories/ProductRepository.cs
- [X] T045 [P] [US1] Create IOcrService interface in backend/src/AgenticShopper.Agents.Receipt/Interfaces/IOcrService.cs
- [X] T046 [US1] Implement AzureDocumentIntelligenceOcrService in backend/src/AgenticShopper.Agents.Receipt/Services/AzureDocumentIntelligenceOcrService.cs
- [X] T047 [P] [US1] Implement PaddleOcrService for local dev in backend/src/AgenticShopper.Agents.Receipt/Services/PaddleOcrService.cs
- [X] T048 [US1] Implement ReceiptAgent with OCR workflow in backend/src/AgenticShopper.Agents.Receipt/ReceiptAgent.cs
- [ ] T049 [US1] Create ReceiptProcessingController with upload endpoint in backend/src/AgenticShopper.Agents.Receipt/Controllers/ReceiptController.cs
- [ ] T050 [US1] Implement receipt image upload to Azure Blob Storage in backend/src/AgenticShopper.Agents.Receipt/Services/ImageStorageService.cs
- [ ] T051 [US1] Implement ReceiptParser to map OCR results to Receipt entity in backend/src/AgenticShopper.Agents.Receipt/Services/ReceiptParser.cs
- [ ] T052 [US1] Create CoordinatorAgent receipt processing workflow in backend/src/AgenticShopper.Coordinator/Services/CoordinatorAgent.cs
- [ ] T053 [US1] Implement receipt status tracking (Pending/Processing/NeedsReview/Verified) in workflow
- [ ] T054 [US1] Add validation for receipt image formats (JPEG, PNG, PDF) and size (<10MB) in controller
- [ ] T055 [US1] Implement confidence score calculation for OCR results in ReceiptParser
- [ ] T056 [P] [US1] Create ReceiptUpload component in frontend/src/components/receipts/ReceiptUpload.tsx
- [ ] T057 [P] [US1] Create ReceiptReview component with edit capabilities in frontend/src/components/receipts/ReceiptReview.tsx
- [ ] T058 [P] [US1] Create ReceiptList component in frontend/src/components/receipts/ReceiptList.tsx
- [ ] T059 [US1] Implement receipt API client in frontend/src/services/api/receiptApi.ts
- [ ] T060 [US1] Create ReceiptsPage with upload and review workflow in frontend/src/pages/ReceiptsPage.tsx
- [ ] T061 [US1] Add receipt image preview functionality in ReceiptReview component
- [ ] T062 [US1] Implement manual correction UI for product names, prices, quantities in ReceiptReview
- [ ] T063 [US1] Add error handling for blurry/low-confidence receipts (FR-003)
- [ ] T064 [US1] Implement receipt re-upload functionality for failed OCR

**Checkpoint**: User Story 1 complete - Receipt upload and OCR extraction working end-to-end

---

## Phase 4: User Story 2 - Product Categorization and Frequency Assignment (Priority: P1)

**Goal**: Auto-categorize products using LLM, calculate purchase frequency, allow user overrides

**Independent Test**: View products from receipts → Verify auto-categorization → Override category → View frequency suggestions → Override frequency → Verify persistence

### Implementation for User Story 2

- [ ] T065 [P] [US2] Implement CategorizationAgent with LLM prompts in backend/src/AgenticShopper.Agents.Categorization/CategorizationAgent.cs
- [ ] T066 [P] [US2] Create CategoryClassifier service in backend/src/AgenticShopper.Agents.Categorization/Services/CategoryClassifier.cs
- [ ] T067 [US2] Implement LLM prompt templates for category suggestion in backend/src/AgenticShopper.Agents.Categorization/Prompts/CategorizationPrompts.cs
- [ ] T068 [US2] Create CategorizationController with suggest/assign endpoints in backend/src/AgenticShopper.Agents.Categorization/Controllers/CategorizationController.cs
- [ ] T069 [US2] Implement category preference storage (FR-009) in ProductRepository
- [ ] T070 [P] [US2] Implement FrequencyAgent in backend/src/AgenticShopper.Agents.Frequency/FrequencyAgent.cs
- [ ] T071 [US2] Implement FrequencyCalculator algorithm (minimum 3 purchases) in backend/src/AgenticShopper.Agents.Frequency/Services/FrequencyCalculator.cs
- [ ] T072 [US2] Create FrequencyController with calculate/override endpoints in backend/src/AgenticShopper.Agents.Frequency/Controllers/FrequencyController.cs
- [ ] T073 [US2] Implement frequency pause functionality (FR-015) in FrequencyAgent
- [ ] T074 [US2] Add product normalized name generation for matching in ProductRepository
- [ ] T075 [US2] Update CoordinatorAgent to orchestrate categorization after receipt processing
- [ ] T076 [US2] Update CoordinatorAgent to orchestrate frequency calculation after categorization
- [ ] T077 [P] [US2] Create ProductCategorization component in frontend/src/components/products/ProductCategorization.tsx
- [ ] T078 [P] [US2] Create FrequencyAssignment component in frontend/src/components/products/FrequencyAssignment.tsx
- [ ] T079 [US2] Implement categorization API client in frontend/src/services/api/categorizationApi.ts
- [ ] T080 [P] [US2] Implement frequency API client in frontend/src/services/api/frequencyApi.ts
- [ ] T081 [US2] Create ProductsPage with categorization and frequency views in frontend/src/pages/ProductsPage.tsx
- [ ] T082 [US2] Add category dropdown with predefined + custom categories in ProductCategorization
- [ ] T083 [US2] Implement frequency selector (Weekly, Fortnightly, Monthly, Quarterly, Annually, Occasional)
- [ ] T084 [US2] Add visual indicators for auto-suggested vs manually-assigned categories
- [ ] T085 [US2] Implement frequency recalculation on new purchases (FR-014)

**Checkpoint**: User Story 2 complete - Product categorization and frequency tracking working independently

---

## Phase 5: User Story 3 - Automated Shopping List Generation (Priority: P2)

**Goal**: Generate shopping lists based on frequency analysis with urgency indicators

**Independent Test**: Request shopping list generation → Verify due items appear → Check urgency levels → Modify list → Save

### Implementation for User Story 3

- [ ] T086 [P] [US3] Create ShoppingList entity in backend/src/AgenticShopper.Core/Models/ShoppingList.cs
- [ ] T087 [P] [US3] Create ShoppingListItem entity in backend/src/AgenticShopper.Core/Models/ShoppingListItem.cs
- [ ] T088 [US3] Add ShoppingLists and ShoppingListItems tables to EF Core migration
- [ ] T089 [US3] Implement ShoppingListRepository in backend/src/AgenticShopper.Data/Repositories/ShoppingListRepository.cs
- [ ] T090 [US3] Implement ListGeneratorAgent in backend/src/AgenticShopper.Agents.ListGenerator/ListGeneratorAgent.cs
- [ ] T091 [US3] Implement RecommendationEngine in backend/src/AgenticShopper.Agents.ListGenerator/Services/RecommendationEngine.cs
- [ ] T092 [US3] Implement UrgencyClassifier (Overdue, DueThisWeek, Upcoming) in backend/src/AgenticShopper.Agents.ListGenerator/Services/UrgencyClassifier.cs
- [ ] T093 [US3] Create ListGeneratorController with generate endpoint in backend/src/AgenticShopper.Agents.ListGenerator/Controllers/ListGeneratorController.cs
- [ ] T094 [US3] Implement frequency-based item selection logic in RecommendationEngine
- [ ] T095 [US3] Add category grouping for shopping list items (FR-018) in RecommendationEngine
- [ ] T096 [US3] Implement urgency-based sorting (overdue first) in list generation
- [ ] T097 [US3] Add "recently purchased outside system" marking functionality (FR-021)
- [ ] T098 [US3] Update CoordinatorAgent with shopping list generation workflow
- [ ] T099 [P] [US3] Create ListGenerator component in frontend/src/components/shopping-lists/ListGenerator.tsx
- [ ] T100 [P] [US3] Create ListEditor component with add/remove/modify in frontend/src/components/shopping-lists/ListEditor.tsx
- [ ] T101 [US3] Implement shopping list API client in frontend/src/services/api/shoppingListApi.ts
- [ ] T102 [US3] Create ShoppingListsPage in frontend/src/pages/ShoppingListsPage.tsx
- [ ] T103 [US3] Add urgency indicators (color coding, icons) in ListEditor
- [ ] T104 [US3] Implement manual item addition to lists in ListEditor
- [ ] T105 [US3] Add quantity adjustment controls in ListEditor
- [ ] T106 [US3] Implement frequency pause toggle in frontend for vacation mode

**Checkpoint**: User Story 3 complete - Automated list generation working with urgency classification

---

## Phase 6: User Story 4 - Promotion and Price Comparison (Priority: P2)

**Goal**: Display promotions from Coles/Woolworths catalogs, compare prices across stores

**Independent Test**: View shopping list → See promotion indicators → Compare prices across stores → View savings suggestions

### Implementation for User Story 4

- [ ] T107 [P] [US4] Create Promotion entity in backend/src/AgenticShopper.Core/Models/Promotion.cs
- [ ] T108 [US4] Add Promotions table to EF Core migration
- [ ] T109 [US4] Implement PromotionRepository in backend/src/AgenticShopper.Data/Repositories/PromotionRepository.cs
- [ ] T110 [US4] Implement PriceAgent in backend/src/AgenticShopper.Agents.PriceComparison/PriceAgent.cs
- [ ] T111 [P] [US4] Implement ColesCatalogService in backend/src/AgenticShopper.Agents.PriceComparison/Services/ColesCatalogService.cs
- [ ] T112 [P] [US4] Implement WoolworthsCatalogService in backend/src/AgenticShopper.Agents.PriceComparison/Services/WoolworthsCatalogService.cs
- [ ] T113 [US4] Implement IStoreCatalogService interface in backend/src/AgenticShopper.Agents.PriceComparison/Interfaces/IStoreCatalogService.cs
- [ ] T114 [US4] Implement PriceOptimizer for savings calculation in backend/src/AgenticShopper.Agents.PriceComparison/Services/PriceOptimizer.cs
- [ ] T115 [US4] Create PriceController with promotions/compare endpoints in backend/src/AgenticShopper.Agents.PriceComparison/Controllers/PriceController.cs
- [ ] T116 [US4] Implement web scraping logic for Coles catalog in ColesCatalogService
- [ ] T117 [US4] Implement web scraping logic for Woolworths catalog in WoolworthsCatalogService
- [ ] T118 [US4] Add Redis caching for promotion data (7-day TTL) in catalog services
- [ ] T119 [US4] Implement weekly promotion refresh background job (IHostedService) in backend/src/AgenticShopper.Agents.PriceComparison/Jobs/PromotionRefreshJob.cs
- [ ] T120 [US4] Implement product name fuzzy matching for promotions in PriceAgent
- [ ] T121 [US4] Add optimal shopping strategy calculation (FR-026) in PriceOptimizer
- [ ] T122 [US4] Update CoordinatorAgent to include price comparison in list generation
- [ ] T123 [P] [US4] Create PriceComparison component in frontend/src/components/shopping-lists/PriceComparison.tsx
- [ ] T124 [US4] Implement price API client in frontend/src/services/api/priceApi.ts
- [ ] T125 [US4] Add promotion indicators (badges, discount %) to ListEditor component
- [ ] T126 [US4] Create price comparison table showing Coles vs Woolworths prices
- [ ] T127 [US4] Add savings summary component with total potential savings
- [ ] T128 [US4] Implement optimal shopping strategy display (which items at which store)
- [ ] T129 [US4] Add promotion expiry date indicators in UI

**Checkpoint**: User Story 4 complete - Price comparison and promotion recommendations working

---

## Phase 7: User Story 5 - Spending Analytics and Budget Tracking (Priority: P3)

**Goal**: Visual dashboards, budget limits, spending alerts

**Independent Test**: View spending trends charts → Set category budgets → Verify alert when threshold reached

### Implementation for User Story 5

- [ ] T130 [P] [US5] Create Budget entity in backend/src/AgenticShopper.Core/Models/Budget.cs
- [ ] T131 [US5] Add Budgets table to EF Core migration
- [ ] T132 [US5] Implement BudgetRepository in backend/src/AgenticShopper.Data/Repositories/BudgetRepository.cs
- [ ] T133 [US5] Implement BudgetAgent in backend/src/AgenticShopper.Agents.Budget/BudgetAgent.cs
- [ ] T134 [US5] Implement SpendingAnalyzer in backend/src/AgenticShopper.Agents.Budget/Services/SpendingAnalyzer.cs
- [ ] T135 [US5] Implement BudgetAlertService in backend/src/AgenticShopper.Agents.Budget/Services/BudgetAlertService.cs
- [ ] T136 [US5] Create BudgetController with track/analytics endpoints in backend/src/AgenticShopper.Agents.Budget/Controllers/BudgetController.cs
- [ ] T137 [US5] Implement spending trend calculation (weekly, monthly, quarterly) in SpendingAnalyzer
- [ ] T138 [US5] Implement budget threshold checking (90% default) in BudgetAlertService
- [ ] T139 [US5] Add Azure Service Bus for async budget alert publishing
- [ ] T140 [US5] Implement budget auto-reset on period end in BudgetAlertService
- [ ] T141 [US5] Update purchase recording to trigger budget tracking
- [ ] T142 [P] [US5] Create SpendingDashboard component with Chart.js in frontend/src/components/analytics/SpendingDashboard.tsx
- [ ] T143 [P] [US5] Create BudgetTracker component in frontend/src/components/analytics/BudgetTracker.tsx
- [ ] T144 [P] [US5] Create Charts component (bar, line, pie) in frontend/src/components/analytics/Charts.tsx
- [ ] T145 [US5] Implement budget API client in frontend/src/services/api/budgetApi.ts
- [ ] T146 [US5] Create AnalyticsPage in frontend/src/pages/AnalyticsPage.tsx
- [ ] T147 [US5] Add spending trend visualizations (line charts) in SpendingDashboard
- [ ] T148 [US5] Add category spending breakdown (pie chart) in SpendingDashboard
- [ ] T149 [US5] Add store spending distribution (bar chart) in SpendingDashboard
- [ ] T150 [US5] Implement budget creation and editing in BudgetTracker
- [ ] T151 [US5] Add budget alert notifications in UI
- [ ] T152 [US5] Add budget progress bars with threshold indicators

**Checkpoint**: User Story 5 complete - Analytics dashboard and budget tracking functional

---

## Phase 8: User Story 6 - Shopping List Management (Priority: P3)

**Goal**: Create/manage multiple lists, real-time collaboration, mark items purchased

**Independent Test**: Create multiple lists → Share with family → Edit concurrently → Mark items purchased → Archive

### Implementation for User Story 6

- [ ] T153 [US6] Add SharedWith JSON field to ShoppingList entity
- [ ] T154 [US6] Implement list archival status tracking in ShoppingListRepository
- [ ] T155 [US6] Update ShoppingListHub for real-time collaboration in backend/src/AgenticShopper.Coordinator/Hubs/ShoppingListHub.cs
- [ ] T156 [US6] Implement list sharing logic with family member access control
- [ ] T157 [US6] Add item purchase marking (IsPurchased flag) in ShoppingListItem
- [ ] T158 [US6] Implement list copy functionality in ShoppingListRepository
- [ ] T159 [US6] Add list archival and restoration endpoints in CoordinatorAgent
- [ ] T160 [P] [US6] Create ListSharing component in frontend/src/components/shopping-lists/ListSharing.tsx
- [ ] T161 [US6] Update ListEditor with real-time sync via SignalR
- [ ] T162 [US6] Add multi-list management UI in ShoppingListsPage
- [ ] T163 [US6] Implement list naming and creation modal
- [ ] T164 [US6] Add item reordering (drag-and-drop) in ListEditor
- [ ] T165 [US6] Implement purchase marking checkbox in list items
- [ ] T166 [US6] Add visual indication of completed items (strikethrough, dim)
- [ ] T167 [US6] Implement list archival UI with archive/restore buttons
- [ ] T168 [US6] Add concurrent edit conflict resolution (last-write-wins)

**Checkpoint**: User Story 6 complete - Multi-list management and collaboration working

---

## Phase 9: User Story 7 - Product Notes and Metadata (Priority: P3)

**Goal**: Add notes and tags to products, filter/search by metadata

**Independent Test**: Add note to product → Add tags → Filter products by tag → Verify notes in list

### Implementation for User Story 7

- [ ] T169 [US7] Add Notes and Tags JSON fields to Product entity (already in data model)
- [ ] T170 [US7] Implement note/tag save functionality in ProductRepository
- [ ] T171 [US7] Add search/filter by tags and notes in ProductRepository query methods
- [ ] T172 [US7] Create ProductController with notes/tags endpoints in backend/src/AgenticShopper.Coordinator/Controllers/ProductController.cs
- [ ] T173 [US7] Implement tag-based product filtering logic
- [ ] T174 [US7] Add notes display in shopping list generation
- [ ] T175 [P] [US7] Create ProductNotes component in frontend/src/components/products/ProductNotes.tsx
- [ ] T176 [US7] Implement product API client in frontend/src/services/api/productApi.ts
- [ ] T177 [US7] Add notes/tags editor in ProductsPage
- [ ] T178 [US7] Implement tag autocomplete for common tags (organic, gluten-free, etc.)
- [ ] T179 [US7] Add tag filtering UI with chip-based selection
- [ ] T180 [US7] Display product notes in shopping list items
- [ ] T181 [US7] Implement notes search functionality

**Checkpoint**: User Story 7 complete - Product notes and metadata functional

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements affecting multiple user stories

- [ ] T182 [P] Add comprehensive error logging across all agents
- [ ] T183 [P] Implement Application Insights telemetry in all services
- [ ] T184 [P] Add request/response logging middleware
- [ ] T185 Add data export (CSV) functionality (FR-049) in CoordinatorAgent
- [ ] T186 Add receipt deletion functionality (FR-050) in ReceiptController
- [ ] T187 [P] Add loading states and skeleton screens in all frontend components
- [ ] T188 [P] Implement toast notifications for user feedback across UI
- [ ] T189 [P] Add responsive design CSS for mobile devices
- [ ] T190 Setup Kubernetes manifests in backend/deployment/kubernetes/
- [ ] T191 [P] Setup Azure Container Apps Bicep templates in backend/deployment/azure/
- [ ] T192 [P] Create CI/CD pipeline in .github/workflows/ci-backend.yml
- [ ] T193 [P] Create CI/CD pipeline in .github/workflows/ci-frontend.yml
- [ ] T194 Update README.md with feature overview and quickstart
- [ ] T195 Run quickstart.md validation (Docker Compose up, verify all services)
- [ ] T196 [P] Add API rate limiting for external-facing endpoints
- [ ] T197 [P] Implement request validation with FluentValidation
- [ ] T198 Security audit (OWASP checks, dependency scanning)
- [ ] T199 Performance profiling and optimization across agents
- [ ] T200 Final integration testing of complete workflows

---

## Dependencies & Execution Order

### Phase Dependencies

1. **Setup (Phase 1)**: No dependencies - start immediately
2. **Foundational (Phase 2)**: Depends on Setup - **BLOCKS all user stories**
3. **User Stories (Phases 3-9)**: All depend on Foundational completion
   - Can execute in parallel with sufficient team capacity
   - Or sequentially by priority: P1 (US1, US2) → P2 (US3, US4) → P3 (US5, US6, US7)
4. **Polish (Phase 10)**: Depends on desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Receipt capture - Independent, can start immediately after Foundational
- **US2 (P1)**: Categorization/Frequency - Builds on US1 entities but independently testable
- **US3 (P2)**: List generation - Uses frequency data from US2, independently testable
- **US4 (P2)**: Price comparison - Enhances US3 lists, independently testable
- **US5 (P3)**: Analytics - Analyzes data from US1-US2, independently testable
- **US6 (P3)**: List management - Enhances US3 lists, independently testable
- **US7 (P3)**: Product notes - Enhances US2 products, independently testable

### Within Each User Story

**Standard sequence**:
1. Create entities (models)
2. Add database migrations
3. Implement repositories
4. Build agent services
5. Create agent controllers
6. Update coordinator orchestration
7. Build frontend components
8. Implement API clients
9. Create page/UI integration

**Parallelization**: Tasks marked [P] within same story can run concurrently

### Parallel Opportunities

**Setup Phase**: T003, T004, T005, T006 can run in parallel

**Foundational Phase**: 
- Entity creation: T010, T011, T012, T013 in parallel
- LLM providers: T017, T018 in parallel
- Middleware/config: T022, T023, T024, T025, T026, T028, T029, T030 in parallel
- Docker: T031, T032 in parallel
- Frontend: T036, T037, T038 in parallel

**User Story 1**:
- Entities: T039, T040, T041 in parallel
- Repositories: T044 parallel with T043
- OCR services: T047 parallel with T046
- Frontend components: T056, T057, T058 in parallel

**User Story 2**:
- Agents: T065, T066, T070 in parallel
- Frontend: T077, T078 in parallel
- API clients: T080 parallel with T079

**All other user stories follow similar parallelization patterns for [P] tasks**

---

## Parallel Execution Examples

### MVP Delivery (User Stories 1 & 2 Only)

**Team of 3 developers**:

1. **Phase 1 & 2** (Week 1-2): All developers work together on Setup + Foundational
2. **Phase 3** (Week 3): 
   - Developer A: Backend (T039-T055)
   - Developer B: Frontend (T056-T064)
   - Developer C: Start US2 backend prep (T065-T069)
3. **Phase 4** (Week 4):
   - Developer A: US2 agents (T070-T076)
   - Developer B: US2 frontend (T077-T085)
   - Developer C: Integration testing US1+US2

**Result**: MVP (P1 stories) deliverable in 4 weeks

### Full Feature (All 7 User Stories)

**Team of 5 developers**:

1. **Foundational** (2 weeks): All hands
2. **P1 Stories** (2 weeks): 
   - Dev 1-2: US1
   - Dev 3-4: US2
   - Dev 5: Setup for US3
3. **P2 Stories** (2 weeks):
   - Dev 1-2: US3
   - Dev 3-4: US4
   - Dev 5: Setup for US5
4. **P3 Stories** (3 weeks):
   - Dev 1-2: US5
   - Dev 3: US6
   - Dev 4: US7
   - Dev 5: Polish/deployment
5. **Polish** (1 week): All hands

**Result**: Complete feature in 10 weeks

---

## Implementation Strategy

### Recommended: MVP First

**Deliver User Stories 1 & 2 (P1 only) as MVP**:

1. Complete Phase 1: Setup (T001-T007)
2. Complete Phase 2: Foundational (T008-T038) - **CRITICAL GATE**
3. Complete Phase 3: User Story 1 (T039-T064)
4. **VALIDATE**: Test receipt upload workflow end-to-end
5. Complete Phase 4: User Story 2 (T065-T085)
6. **VALIDATE**: Test categorization and frequency independently
7. **DEPLOY MVP**: Receipt capture + categorization working
8. Gather feedback before proceeding to P2/P3 stories

### Incremental Delivery

After MVP, add one story at a time:

1. **Week 5-6**: Add US3 (List generation) → Deploy → Demo
2. **Week 7-8**: Add US4 (Price comparison) → Deploy → Demo
3. **Week 9-10**: Add US5 (Analytics) → Deploy → Demo
4. **Week 11**: Add US6 (List management) → Deploy → Demo
5. **Week 12**: Add US7 (Notes) → Deploy → Demo
6. **Week 13**: Polish → Final deployment

Each increment adds value without breaking previous features.

---

## Notes

- **Task Count**: 200 tasks total
  - Setup: 7 tasks
  - Foundational: 31 tasks (BLOCKS all stories)
  - User Story 1 (P1): 26 tasks
  - User Story 2 (P1): 21 tasks
  - User Story 3 (P2): 21 tasks
  - User Story 4 (P2): 23 tasks
  - User Story 5 (P3): 23 tasks
  - User Story 6 (P3): 16 tasks
  - User Story 7 (P3): 13 tasks
  - Polish: 19 tasks

- **[P] markers**: Indicate tasks that can run in parallel (different files, no blocking dependencies)
- **[US#] labels**: Map tasks to user stories for traceability
- **File paths**: All tasks include specific file locations per plan.md structure
- **Tests**: NOT included (not explicitly requested in spec.md)
- **Independent testing**: Each user story designed to be testable in isolation
- **Checkpoint validation**: Stop at each checkpoint to verify story works before proceeding
