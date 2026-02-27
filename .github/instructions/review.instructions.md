---
applyTo: "docs/**"
excludeAgent: "coding-agent"
---

# Document Generation Standards

## Language

- All review comments shall be written in Japanese.
- Abbreviations and technical terms shall include their full form upon first use (e.g., SRS (Software Requirements Specification)).

## ISO Standards Compliance

All generated documents shall conform to the format and structure defined by the following system development ISO standards.

### Applicable Standards

| Standard | Description |
|---|---|
| ISO/IEC/IEEE 12207 | Software Life Cycle Processes |
| ISO/IEC/IEEE 15288 | System Life Cycle Processes |
| ISO/IEC/IEEE 29148 | Requirements Engineering (Requirements Specification Format) |
| ISO/IEC/IEEE 15289 | Life Cycle Information Products (Documentation Content) |

### Common Document Format

Every document shall include the following sections.

1. **Cover Page**
   - Document title
   - Document number and version
   - Date of creation / date of revision
   - Author / approver
2. **Revision History**
   - Record version, date, description of change, and author in tabular form
3. **Table of Contents**
4. **1. Introduction**
   - 1.1 Purpose
   - 1.2 Scope
   - 1.3 Terms and Definitions
   - 1.4 Referenced Documents
5. **2. Body** (content per document type)
6. **Appendices** (as needed)

### Document Quality Criteria

- Each section shall be structured with hierarchical numbering (1, 1.1, 1.1.1, etc.).
- Requirements and specifications shall be assigned a unique identifier (e.g., REQ-001).
- All figures and tables shall have a caption and a number (e.g., Figure 1, Table 2).
- Ambiguous expressions (e.g., "etc.", "as appropriate") shall be avoided; descriptions shall be specific and verifiable.
