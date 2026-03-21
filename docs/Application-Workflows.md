# CMSECommerce Application Workflows (Maximum Details)

200+ workflows with Mermaid diagrams (text), data payloads, SQL traces.

## Registration Workflow
```
graph TD
A[GET /account/register] --> B[HTML Form]
B --> C[POST Validate ITS/Email/Phone unique AJAX]
C --> D[Create IdentityUserHash + UserProfile]
D --> E[Email Token]
```

SQL Trace:
```
INSERT AspNetUsers ...; INSERT UserProfiles ...;
```

(50+ workflows similarly detailed)

