# Feature Specification: Smart Shopping Pattern Analyzer & Recommender

**Feature Branch**: `001-shopping-analyzer`  
**Created**: December 13, 2025  
**Status**: Draft  
**Input**: User description: "Build a shopping pattern analyzer and recommender based on previously shopped items using Agentic AI capabilities. Perform optical character recognition on the bills from grocery stores. classify products into different categories such as dairy, fresh produce, laundry, groceries. Also assign a frequency to the shopped item as weekly, monthly, quarterly or annually. Considering the frequency of the items recommend the items for next shopping trip. As part of the suggestions or recommendations the system should look at the online catalog to suggest products which are on promotion there should be ability to compare the products across multiple stores such as Coles or woolworths. The system should provide dashboards to visualize spending patterns and set budgets across categories and track them over a period of time. The system should provide ability to create and modify shopping lists. The system should have an ability to add notes or metadata for specific products so that they can be used for future analysis Problem: help to build weekly and monthly shopping lists by analysing the historic shopping patterns. Users: family shoppers who shop regularly and look to optimize the savings using discounts and offers. Key flows: add bills, tag products to categories, assign buying frequency to products, create automated shopping lists, optimize lists by comparing across multiple shops. Out of scope: actual shopping of the items"

## Clarifications

### Session 2025-12-13

- Q: How frequently should the system update promotion data - real-time when list is viewed, daily automated updates, or manual refresh by user? → A: Weekly automated updates (promotions typically change weekly)
- Q: What sharing mechanism should be used for shopping lists - email link, in-app user invitation, SMS, or public URL? → A: Lists are persisted in the system and family members access them via the user interface
- Q: What authentication and family account model should the system use - single family account with multiple profiles, individual accounts with family linking, or individual accounts with list sharing? → A: Single family account with multiple user profiles

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bill Capture and Product Recognition (Priority: P1)

Family shoppers can upload or photograph their grocery store receipts, and the system automatically extracts product names, prices, store name, and purchase date. Users can review and confirm the extracted information.

**Why this priority**: This is the foundation of the entire system - without capturing shopping history, no analysis or recommendations can be made. It delivers immediate value by digitizing paper receipts for record-keeping.

**Independent Test**: Can be fully tested by uploading a grocery receipt and verifying that product names, prices, and store details are accurately extracted and displayed for user confirmation. Delivers value as a digital receipt storage system.

**Acceptance Scenarios**:

1. **Given** a user has a physical grocery receipt from Coles, **When** they photograph it using the system, **Then** the system extracts all product names, individual prices, total amount, store name, and purchase date with at least 90% accuracy
2. **Given** extracted receipt data contains errors, **When** the user reviews the results, **Then** they can manually correct product names, prices, or remove incorrect entries
3. **Given** a receipt image is blurry or poorly lit, **When** the system processes it, **Then** it notifies the user about low confidence items and requests re-upload or manual entry
4. **Given** a user uploads a PDF receipt from an online order, **When** the system processes it, **Then** it extracts the same information as from photographed receipts

---

### User Story 2 - Product Categorization and Frequency Assignment (Priority: P1)

Shoppers can view their captured products organized into categories (dairy, fresh produce, laundry, groceries, etc.) and assign or adjust purchase frequency (weekly, monthly, quarterly, annually) to each product.

**Why this priority**: Categorization and frequency are essential for generating useful recommendations and budgets. This creates the structured data needed for all analytical features. Together with P1 Story 1, this forms a complete basic tracking system.

**Independent Test**: Can be tested by reviewing a list of products from uploaded receipts, assigning them to categories, setting frequencies, and verifying the system remembers these assignments for future purchases. Delivers value as an organized expense tracker.

**Acceptance Scenarios**:

1. **Given** newly captured products from a receipt, **When** the user views them, **Then** the system suggests appropriate categories based on product names with at least 80% accuracy
2. **Given** a product has been purchased multiple times, **When** the user views the product, **Then** the system automatically suggests a frequency based on historical purchase intervals
3. **Given** a user disagrees with auto-categorization, **When** they manually assign a category, **Then** the system remembers this preference for future purchases of the same product
4. **Given** seasonal items like holiday products, **When** assigning frequency, **Then** the user can mark them as "seasonal/occasional" rather than regular intervals
5. **Given** a product exists in multiple categories (e.g., organic milk could be dairy or organic), **When** categorizing, **Then** the user can assign it to one primary category

---

### User Story 3 - Automated Shopping List Generation (Priority: P2)

Shoppers can generate shopping lists automatically based on purchase frequency analysis. The system suggests items that are due for repurchase based on historical patterns.

**Why this priority**: This is the core value proposition - using AI to predict needs. It builds on the data from P1 stories and provides significant time-saving benefits.

**Independent Test**: Can be tested by having a user with 2-3 months of shopping history request a shopping list, and verifying it includes items whose frequency indicates they're due for purchase. Delivers value as an intelligent shopping assistant.

**Acceptance Scenarios**:

1. **Given** a user has tracked weekly milk purchases for 6 weeks, **When** 6-7 days have passed since last purchase, **Then** milk appears on the suggested shopping list
2. **Given** multiple products are due for purchase, **When** generating a list, **Then** items are grouped by category and sorted by urgency (overdue items first)
3. **Given** a user manually purchased an item outside the system, **When** they mark it as "recently purchased", **Then** it's removed from the current list and the next suggestion date adjusts accordingly
4. **Given** holiday periods or vacations, **When** the user is away, **Then** they can pause frequency tracking to avoid incorrect suggestions
5. **Given** a generated shopping list, **When** the user reviews it, **Then** they can add manual items, remove suggestions, or adjust quantities

---

### User Story 4 - Promotion and Price Comparison (Priority: P2)

Shoppers can view which items on their shopping list are currently on promotion at different stores (Coles, Woolworths) and see price comparisons to optimize savings.

**Why this priority**: This directly addresses the user goal of "optimizing savings using discounts and offers." It provides actionable cost-saving information.

**Independent Test**: Can be tested by creating a shopping list, then viewing promotion indicators and price comparisons across stores. Delivers value as a price-shopping tool.

**Acceptance Scenarios**:

1. **Given** a shopping list with 5 items, **When** checking for promotions, **Then** the system displays which stores have each item on sale and the discount percentage
2. **Given** milk is $4.50 at Coles and $4.00 at Woolworths, **When** comparing prices, **Then** the system highlights Woolworths as the better price and shows potential savings
3. **Given** multiple stores have different items on promotion, **When** viewing the list, **Then** the system suggests an optimal shopping strategy (e.g., "Buy dairy at Woolworths, buy produce at Coles for total saving of $12")
4. **Given** promotion data is refreshed weekly, **When** a new promotional cycle begins, **Then** the system automatically updates prices and discount indicators for all affected items
5. **Given** a promotion has expired, **When** viewing the list, **Then** the system removes the promotion indicator and updates prices

---

### User Story 5 - Spending Analytics and Budget Tracking (Priority: P3)

Shoppers can view visual dashboards showing spending patterns by category over time, set monthly/weekly budgets per category, and receive alerts when approaching budget limits.

**Why this priority**: While valuable for financial awareness, this is enhancement functionality. Users can achieve their core goal (optimized shopping) without detailed analytics.

**Independent Test**: Can be tested by viewing spending history over several weeks, creating category budgets, and verifying visual charts and budget alerts work correctly. Delivers value as a financial planning tool.

**Acceptance Scenarios**:

1. **Given** 3 months of shopping history, **When** viewing the dashboard, **Then** the user sees monthly spending trends by category in bar or line charts
2. **Given** a user sets a $200 monthly budget for groceries, **When** spending reaches $180 (90%), **Then** they receive a notification warning of approaching limit
3. **Given** weekly shopping data, **When** viewing analytics, **Then** the user can see average spending per week, highest spending categories, and spending variance
4. **Given** multiple stores in purchase history, **When** analyzing spending, **Then** the dashboard shows spending distribution across stores
5. **Given** a budget period ends, **When** the new period starts, **Then** budgets reset automatically and the user can see previous period performance

---

### User Story 6 - Shopping List Management (Priority: P3)

Shoppers can create multiple named shopping lists manually, combine auto-generated suggestions with manual items, share lists with family members, and mark items as purchased.

**Why this priority**: This enhances the core shopping list functionality but users can accomplish primary goals with basic list features from P2 stories.

**Independent Test**: Can be tested by creating multiple lists (e.g., "Weekly Shop", "Party Supplies"), adding items, sharing via link or email, and marking purchases. Delivers value as a collaborative planning tool.

**Acceptance Scenarios**:

1. **Given** a user wants to plan for different occasions, **When** they create lists, **Then** they can maintain separate lists like "Regular Weekly", "Christmas Shopping", "Pharmacy Items"
2. **Given** an auto-generated list exists, **When** the user creates a manual list, **Then** they can copy suggested items into their manual list
3. **Given** a shopping list, **When** sharing with a family member, **Then** the recipient can view and edit the list in real-time
4. **Given** a user is at the store, **When** they pick up an item, **Then** they can mark it as purchased and it visually indicates completion
5. **Given** a completed shopping trip, **When** all items are marked purchased, **Then** the user can archive the list for historical reference

---

### User Story 7 - Product Notes and Metadata (Priority: P3)

Shoppers can add notes and custom metadata to individual products (e.g., "Brand preference: Pura milk", "Organic only", "Store location: Aisle 3") for future reference and analysis.

**Why this priority**: This is a nice-to-have feature that improves user experience but isn't essential for core functionality.

**Independent Test**: Can be tested by adding various notes to products, filtering/searching by notes, and verifying notes appear in recommendations. Delivers value as a personalized shopping memory aid.

**Acceptance Scenarios**:

1. **Given** a product in the system, **When** adding a note, **Then** the user can enter free-text notes up to 500 characters and attach tags
2. **Given** products with brand preferences noted, **When** generating shopping lists, **Then** the system includes the preferred brand in the recommendation
3. **Given** products with dietary tags (e.g., "gluten-free", "organic"), **When** searching, **Then** the user can filter their product history by these tags
4. **Given** multiple family members using the system, **When** one adds notes, **Then** all members see these notes when viewing that product
5. **Given** a product note mentions a store location, **When** generating a list for that store, **Then** notes are displayed to help the user find items quickly

---

### Edge Cases

- What happens when a receipt is in a different language or format than expected (e.g., handwritten receipts, non-English text)?
- How does the system handle duplicate products with different brands (e.g., "Milk" from different brands tracked separately or merged)?
- What happens when a store changes its naming or merges (e.g., if Coles rebrands a location)?
- How does the system handle products that are purchased irregularly but not seasonal (e.g., replacing kitchen items when they break)?
- What happens when two receipts from different stores purchased on the same day contain the same product?
- How does the system handle partial quantities (e.g., buying 500g when usually buying 1kg)?
- What happens if a user's shopping patterns change dramatically (e.g., switching to a new diet)?
- How does the system handle price fluctuations for the same product over time?
- What happens when online catalogs are unavailable or don't have pricing data?
- How does the system handle products that are discontinued or replaced with different versions?

## Requirements *(mandatory)*

### Functional Requirements

**Bill Processing:**
- **FR-001**: System MUST accept receipt images in JPEG, PNG, and PDF formats up to 10MB in size
- **FR-002**: System MUST extract product names, individual prices, quantities, store name, purchase date, and total amount from receipts using optical character recognition
- **FR-003**: System MUST display extracted data with confidence indicators showing which items may need manual verification
- **FR-004**: System MUST allow users to manually correct, add, or delete any extracted product information
- **FR-005**: System MUST retain original receipt images for future reference and re-processing

**User Authentication and Family Accounts:**
- **FR-005a**: System MUST support a single family account model where one household shares a common account
- **FR-005b**: System MUST allow multiple user profiles within a family account for individual family members
- **FR-005c**: System MUST attribute actions (receipt uploads, list creation, notes) to individual user profiles while maintaining shared access to all household data

**Product Categorization:**
- **FR-006**: System MUST support predefined categories including: Dairy, Fresh Produce, Meat & Seafood, Bakery, Pantry & Groceries, Frozen Foods, Beverages, Household & Cleaning, Personal Care, Pet Supplies, and Other
- **FR-007**: System MUST automatically suggest categories for products based on product name analysis
- **FR-008**: System MUST allow users to manually assign or change product categories
- **FR-009**: System MUST remember user's category preferences for specific products across future purchases
- **FR-010**: System MUST allow users to create custom categories beyond the predefined list

**Purchase Frequency:**
- **FR-011**: System MUST support frequency classifications: Weekly, Fortnightly, Monthly, Quarterly, Annually, and Occasional
- **FR-012**: System MUST automatically calculate suggested frequency based on purchase interval patterns (minimum 3 purchases)
- **FR-013**: System MUST allow users to manually set or override frequency for any product
- **FR-014**: System MUST recalculate frequency suggestions as more purchase data accumulates
- **FR-015**: System MUST allow users to pause frequency tracking for specific products temporarily

**Shopping List Generation:**
- **FR-016**: System MUST generate suggested shopping lists based on frequency analysis and time since last purchase
- **FR-017**: System MUST indicate urgency levels for list items (overdue, due this week, upcoming)
- **FR-018**: System MUST group list items by product category for easier shopping
- **FR-019**: System MUST allow users to accept, reject, or modify suggested items
- **FR-020**: System MUST support manual addition of items not in purchase history
- **FR-021**: System MUST allow users to mark items as "recently purchased outside system" to adjust next suggestion date

**Promotion and Price Comparison:**
- **FR-022**: System MUST integrate with online catalogs from Coles and Woolworths to retrieve current promotional pricing
- **FR-023**: System MUST display promotion indicators (discount percentage, sale price) for items on shopping lists
- **FR-024**: System MUST show price comparison across stores for items on the shopping list
- **FR-025**: System MUST calculate potential total savings when shopping at different stores
- **FR-026**: System MUST suggest optimal shopping strategy across multiple stores based on prices and promotions
- **FR-027**: System MUST update promotion data automatically on a weekly basis, as store promotions typically operate on weekly cycles

**Spending Analytics:**
- **FR-028**: System MUST display spending trends over time (weekly, monthly, quarterly views)
- **FR-029**: System MUST visualize spending by category using charts (bar charts, pie charts, line graphs)
- **FR-030**: System MUST calculate and display average spending per category per week/month
- **FR-031**: System MUST show spending distribution across different stores
- **FR-032**: System MUST allow users to set budget limits per category (weekly or monthly)
- **FR-033**: System MUST send alerts when spending approaches budget thresholds (configurable percentage, default 90%)
- **FR-034**: System MUST track budget performance over time and show historical compliance

**List Management:**
- **FR-035**: System MUST allow users to create multiple named shopping lists
- **FR-036**: System MUST support manual addition, removal, and reordering of items in lists
- **FR-037**: System MUST allow users to mark items as purchased/completed
- **FR-038**: System MUST allow users to copy items between lists
- **FR-039**: System MUST persist shopping lists and allow family members to access shared lists through the user interface
- **FR-040**: System MUST support real-time collaborative editing of shared lists
- **FR-041**: System MUST allow users to archive completed lists
- **FR-042**: System MUST allow users to restore or reference archived lists

**Product Notes and Metadata:**
- **FR-043**: System MUST allow users to add free-text notes up to 500 characters per product
- **FR-044**: System MUST support custom tags for products (e.g., "organic", "gluten-free", "brand-preference")
- **FR-045**: System MUST display product notes when viewing shopping lists or product details
- **FR-046**: System MUST allow filtering and searching products by tags and notes
- **FR-047**: System MUST share product notes and tags across all users who have access to the same shopping lists

**Data Management:**
- **FR-048**: System MUST persist all receipt data, categorizations, frequencies, and user preferences
- **FR-049**: System MUST allow users to export their shopping data in CSV format
- **FR-050**: System MUST allow users to delete individual receipts or products from their history
- **FR-051**: System MUST maintain data consistency when products are edited or merged

### Key Entities

- **Receipt**: Represents a shopping transaction containing purchase date, store name, total amount, receipt image reference, and associated line items
- **Product**: Represents a unique product with name, category, average price, purchase frequency, notes/tags, and relationships to purchases across multiple receipts
- **Purchase**: Represents a single product purchase instance linking a product to a receipt with specific price, quantity, and date
- **Shopping List**: Named collection of items (both suggested and manual) with creation date, status (active/archived), sharing permissions, and item completion states
- **Store**: Represents a retail location (Coles, Woolworths, etc.) with name, location, and relationship to receipts and pricing catalogs
- **Category**: Product grouping (Dairy, Produce, etc.) with name and description
- **Budget**: User-defined spending limit associated with a category and time period (weekly/monthly) with current spending tracked
- **Promotion**: Limited-time pricing information for specific products at specific stores with start date, end date, discount amount, and sale price
- **User Profile**: Individual family member profile within a family account, with name and action attribution for tracking who performed specific operations
- **Family Account**: Household-level account containing multiple user profiles and all shared data (receipts, products, lists, budgets)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can capture and review a grocery receipt in under 2 minutes from photograph to confirmed data
- **SC-002**: System achieves 90% or higher accuracy in product name and price extraction from clearly photographed receipts
- **SC-003**: Users can generate a weekly shopping list in under 30 seconds
- **SC-004**: 80% of auto-suggested shopping list items are accepted by users without modification
- **SC-005**: Users save an average of 15% on shopping costs when using promotion recommendations compared to their historical spending
- **SC-006**: Users can compare prices across 2 stores and identify savings opportunities in under 1 minute
- **SC-007**: System processes and categorizes products from a new receipt with 85% automatic categorization accuracy
- **SC-008**: Users successfully share and collaborate on shopping lists with family members with zero data loss
- **SC-009**: Budget tracking alerts are triggered within 1 hour of reaching the threshold spending level
- **SC-010**: 90% of users with 4+ weeks of data report that automated shopping lists accurately reflect their needs
- **SC-011**: System supports concurrent usage by 1000+ users without performance degradation
- **SC-012**: Users can create, edit, and finalize a shopping list in under 3 minutes
