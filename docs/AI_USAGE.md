# AI Usage

AI-assisted development tools included:

- ChatGPT
- GitHub Copilot / Copilot Chat
- Codex / OpenAI coding agent

They were used for general tasks including:

- code generation and review
- creation and adjustment of automated tests
- debugging and error analysis
- edge-case review
- Docker configuration and validation
- HTTP contract and error-handling review
- technical documentation support

All AI-assisted output was manually reviewed and validated through automated tests, manual testing, and execution of the running application.

Validation included scenarios such as:

- stale UI state
- 400 vs 409 response semantics
- max-length inputs reaching persistence and producing an unexpected 500 response
- Docker cold start
- idempotency
- concurrency

## Information handling

- No production credentials or production secrets were intentionally provided to AI tools.
- Example or local configuration values were used where configuration context was required.
- Secrets are kept outside source control.
- Sensitive request payloads are not intentionally included in application logs.
