using RAG_RBAC.Models;

namespace RAG_RBAC.Services;

/// <summary>
/// Provides a curated set of sample documents across all RBAC role levels.
/// Used to seed the Pinecone vector store for demonstration and testing purposes.
/// </summary>
/// <remarks>
/// Documents are distributed across five access levels (Intern through Admin) and
/// multiple departments (HR, Engineering, Finance, Strategy, Security, etc.) to
/// demonstrate how RBAC filtering produces different search results for different users.
/// </remarks>
public static class SeedData
{
    /// <summary>
    /// Returns the complete list of sample documents for seeding the vector store.
    /// Each document includes a <see cref="IngestRequest.MinimumAccessRole"/> that determines
    /// which users can retrieve it during RAG queries.
    /// </summary>
    /// <returns>A list of 23 <see cref="IngestRequest"/> records spanning all role levels.</returns>
    public static List<IngestRequest> GetSampleDocuments() => new()
    {
        // ── Intern-level (accessible to all authenticated users) ─
        new IngestRequest(
            Title: "Company Onboarding Guide",
            Content: "Welcome to Contoso! Our office hours are 9 AM to 5 PM. "
                   + "The cafeteria is on the 2nd floor. Wi-Fi password is on your welcome card. "
                   + "Please complete your HR paperwork within the first week.",
            Source: "HR Portal",
            MinimumAccessRole: "Intern",
            Department: "HR"
        ),
        new IngestRequest(
            Title: "Coding Standards",
            Content: "All C# code must follow Microsoft naming conventions. "
                   + "Use async/await for I/O operations. Write unit tests for all public methods. "
                   + "PRs require at least one approval before merging.",
            Source: "Engineering Wiki",
            MinimumAccessRole: "Intern",
            Department: "Engineering"
        ),
        new IngestRequest(
            Title: "Leave & Holidays Policy",
            Content: "Employees get 18 paid leaves per year plus 12 public holidays. "
                   + "Leave requests must be submitted at least 3 days in advance via the HR portal. "
                   + "Sick leave requires a medical certificate if exceeding 2 consecutive days. "
                   + "Unused leaves can be carried forward up to 5 days to the next year.",
            Source: "HR Portal",
            MinimumAccessRole: "Intern",
            Department: "HR"
        ),
        new IngestRequest(
            Title: "Internal Communication Tools",
            Content: "We use Microsoft Teams for daily communication and Slack for cross-team channels. "
                   + "Outlook is the official email platform. All meetings must have a Teams link. "
                   + "Use the #general channel for company-wide announcements. "
                   + "Confluence is used for long-form documentation and knowledge sharing.",
            Source: "IT Helpdesk",
            MinimumAccessRole: "Intern",
            Department: "IT"
        ),
        new IngestRequest(
            Title: "Expense Reimbursement Process",
            Content: "Submit expense claims within 30 days of the transaction via the Finance portal. "
                   + "Attach original receipts or digital copies. Claims above $100 need manager approval. "
                   + "Reimbursements are processed in the next payroll cycle. "
                   + "Travel expenses must follow the company travel policy guidelines.",
            Source: "Finance Portal",
            MinimumAccessRole: "Intern",
            Department: "Finance"
        ),

        // ── Developer-level (Intern + Developer access) ─────────
        new IngestRequest(
            Title: "Azure Infrastructure Guide",
            Content: "Our production environment runs on Azure App Service with P2v3 SKU. "
                   + "The primary database is Azure SQL with geo-replication to East US 2. "
                   + "Connection strings are stored in Azure Key Vault (vault name: contoso-prod-kv).",
            Source: "DevOps Docs",
            MinimumAccessRole: "Developer",
            Department: "Engineering"
        ),
        new IngestRequest(
            Title: "CI/CD Pipeline Architecture",
            Content: "We use Azure DevOps Pipelines with YAML definitions. "
                   + "The build pipeline runs on self-hosted agents in our Azure Kubernetes cluster. "
                   + "Deployments follow blue-green strategy with automatic rollback on health check failures.",
            Source: "DevOps Docs",
            MinimumAccessRole: "Developer",
            Department: "Engineering"
        ),
        new IngestRequest(
            Title: "API Rate Limiting & Throttling",
            Content: "Public APIs are rate-limited to 1000 requests per minute per API key. "
                   + "Internal service-to-service calls use a higher limit of 10,000 RPM. "
                   + "Rate limit headers (X-RateLimit-Remaining, X-RateLimit-Reset) are included in responses. "
                   + "Exceeding limits returns HTTP 429 with a Retry-After header.",
            Source: "Engineering Wiki",
            MinimumAccessRole: "Developer",
            Department: "Engineering"
        ),
        new IngestRequest(
            Title: "Database Schema & Migration Guide",
            Content: "We use Entity Framework Core with code-first migrations. "
                   + "All migration scripts must be reviewed before applying to production. "
                   + "The Products table has 2.3M rows with indexes on SKU and CategoryId. "
                   + "Use read replicas for reporting queries to avoid impacting production writes.",
            Source: "DevOps Docs",
            MinimumAccessRole: "Developer",
            Department: "Engineering"
        ),
        new IngestRequest(
            Title: "Logging & Monitoring Standards",
            Content: "Application logs are shipped to Azure Monitor and Log Analytics workspace. "
                   + "Use structured logging with Serilog. Include correlation IDs in all log entries. "
                   + "Alerts are configured in PagerDuty for P1/P2 incidents. "
                   + "Dashboard for service health is available at grafana.contoso.internal.",
            Source: "DevOps Docs",
            MinimumAccessRole: "Developer",
            Department: "Engineering"
        ),

        // ── Manager-level (Intern + Developer + Manager access) ─
        new IngestRequest(
            Title: "Q4 Budget Allocations",
            Content: "Engineering budget for Q4 2025 is $2.4M. Cloud infrastructure: $800K. "
                   + "Headcount: 3 new senior engineers approved. Training budget: $50K per team. "
                   + "The AI/ML initiative has a separate $500K allocation from the innovation fund.",
            Source: "Finance Portal",
            MinimumAccessRole: "Manager",
            Department: "Finance"
        ),
        new IngestRequest(
            Title: "Team Performance Review Summary",
            Content: "Engineering team velocity increased 23% in Q3. "
                   + "Two team members are on performance improvement plans. "
                   + "Three promotions recommended for the next cycle: Backend, DevOps, and QA leads.",
            Source: "HR Portal",
            MinimumAccessRole: "Manager",
            Department: "HR"
        ),
        new IngestRequest(
            Title: "Hiring Pipeline Status",
            Content: "Currently 14 open positions across Engineering, Product, and Design. "
                   + "Average time-to-hire is 34 days. Offer acceptance rate is 78%. "
                   + "Top sourcing channels: LinkedIn (45%), referrals (30%), job boards (25%). "
                   + "Recruiting budget remaining for FY2026: $120K out of $200K.",
            Source: "HR Portal",
            MinimumAccessRole: "Manager",
            Department: "HR"
        ),
        new IngestRequest(
            Title: "Product Roadmap Q1-Q2 2026",
            Content: "Q1 focus: AI-powered search feature launch targeting 15% engagement uplift. "
                   + "Q2 focus: Mobile app redesign with new design system. "
                   + "Tech debt sprint planned for March — 20% engineering capacity allocated. "
                   + "Customer-requested features prioritized: SSO integration, bulk export, advanced filters.",
            Source: "Product Portal",
            MinimumAccessRole: "Manager",
            Department: "Product"
        ),
        new IngestRequest(
            Title: "Vendor Contract Renewals",
            Content: "AWS contract renewal due April 2026 — current spend $1.2M/year. "
                   + "Salesforce license renewal in June — 250 seats at $150/seat/month. "
                   + "Negotiate 15% discount on DataDog renewal based on multi-year commitment. "
                   + "Legal review required for all contracts exceeding $100K annually.",
            Source: "Finance Portal",
            MinimumAccessRole: "Manager",
            Department: "Finance"
        ),

        // ── Director-level (Intern through Director access) ─────
        new IngestRequest(
            Title: "M&A Target Analysis",
            Content: "Three acquisition targets identified in the AI tooling space. "
                   + "Target A (DataForge) valued at $45M with 200 enterprise customers. "
                   + "Target B (NeuralOps) valued at $28M, strong IP portfolio. "
                   + "Board approval required before LOI stage.",
            Source: "Strategy Docs",
            MinimumAccessRole: "Director",
            Department: "Strategy"
        ),
        new IngestRequest(
            Title: "Board Meeting Minutes - March 2026",
            Content: "Revenue grew 18% YoY to $42M in FY2025. EBITDA margin improved to 22%. "
                   + "Board approved Series C fundraising of $60M at $400M pre-money valuation. "
                   + "International expansion to UK and Germany greenlit for H2 2026. "
                   + "CEO succession planning discussion tabled for next quarter.",
            Source: "Board Portal",
            MinimumAccessRole: "Director",
            Department: "Executive"
        ),
        new IngestRequest(
            Title: "Competitor Intelligence Report",
            Content: "Main competitor AcmeTech launched AI assistant feature — early reviews mixed. "
                   + "Competitor pricing undercuts us by 20% on enterprise tier. "
                   + "Our NPS score (72) significantly higher than industry average (45). "
                   + "Patent filing by competitor in automated workflow space — legal reviewing overlap.",
            Source: "Strategy Docs",
            MinimumAccessRole: "Director",
            Department: "Strategy"
        ),
        new IngestRequest(
            Title: "Organizational Restructuring Plan",
            Content: "Proposal to merge Platform and Infrastructure teams into a single Cloud Engineering org. "
                   + "New VP of Engineering role to be created — external search initiated. "
                   + "Estimated annual savings of $1.8M from reduced management overhead. "
                   + "Transition timeline: announce in April, complete by end of June 2026.",
            Source: "HR Portal",
            MinimumAccessRole: "Director",
            Department: "HR"
        ),

        // ── Admin-level (full access to all documents) ──────────
        new IngestRequest(
            Title: "Security Incident Response Playbook",
            Content: "In case of a data breach: immediately isolate affected systems. "
                   + "Contact legal (ext 4400) and the CISO within 15 minutes. "
                   + "Regulatory notification required within 72 hours for GDPR-affected data. "
                   + "Root cause analysis must be completed within 5 business days.",
            Source: "Security Portal",
            MinimumAccessRole: "Admin",
            Department: "Security"
        ),
        new IngestRequest(
            Title: "Production Access Credentials Rotation",
            Content: "All production service account passwords must be rotated every 90 days. "
                   + "Current root credentials for Azure subscription stored in CyberArk vault ID: PROD-AZ-001. "
                   + "AWS cross-account role ARN: arn:aws:iam::123456789012:role/ContosoProdAdmin. "
                   + "Emergency break-glass account: admin@contoso-emergency.onmicrosoft.com.",
            Source: "Security Portal",
            MinimumAccessRole: "Admin",
            Department: "Security"
        ),
        new IngestRequest(
            Title: "Data Retention & Compliance Policy",
            Content: "Customer PII must be deleted within 30 days of account closure per GDPR Article 17. "
                   + "Financial records retained for 7 years per SOX compliance. "
                   + "Audit logs must be immutable and stored for minimum 1 year. "
                   + "Annual SOC 2 Type II audit scheduled for September 2026 — prep begins July.",
            Source: "Legal Portal",
            MinimumAccessRole: "Admin",
            Department: "Legal"
        ),
        new IngestRequest(
            Title: "Disaster Recovery Plan",
            Content: "RPO target: 15 minutes. RTO target: 1 hour for critical services. "
                   + "Failover region: Azure West US 2. DR drills conducted quarterly. "
                   + "Last DR test on Feb 2026 achieved RTO of 47 minutes. "
                   + "Backup encryption key stored in HSM with dual-custody access control.",
            Source: "Security Portal",
            MinimumAccessRole: "Admin",
            Department: "Security"
        )
    };
}
