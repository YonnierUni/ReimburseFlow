# Assumptions and Future Considerations

## Assumptions

- Authentication and authorization are not currently implemented.
- Pagination is not currently required.
- Future `expenseDate` values are allowed; no business rule currently restricts future dates.
- Amounts are stored as decimal values without assigning a specific currency.
- Initial categories are:
  - `Transportation`
  - `Food`
  - `Accommodation`
  - `Other`
- `EmployeeId` + `ReceiptNumber` defines the duplicate business key.
- The API is the source of truth for reimbursement state.
- Timestamps are stored in UTC.

## High-value reimbursement consideration

Some high-value reimbursements may require an additional validation step before approval in the future. No threshold, currency rule, validation flow, approval role, or additional state is currently defined, so none of those behaviors are implemented yet.

### Interpretation

The current reimbursement lifecycle remains:

`Pending -> Approved | Rejected`

No speculative rule is introduced until the business policy is formally defined.

### Pending validation

Before implementing this capability, the following must be defined:

- What qualifies as high value.
- Which monetary context applies.
- What additional validation is required.
- Who performs that validation.
- Whether the current lifecycle needs additional states or transitions.
- Whether additional audit data must be stored.

### How the design supports it

The current architecture keeps business rules outside controllers and separates Domain, Application, API, and persistence responsibilities.

A future high-value policy can therefore be introduced in the Domain or Application layer without rewriting the current API or reimbursement flow unless the confirmed business rule actually requires those changes.

The goal is to preserve the current behavior while keeping the rule extensible through the existing architectural boundaries.

### Future implementation

Once the business policy is defined:

- Add a dedicated high-value validation policy.
- Evaluate it from the Application layer before approval.
- Keep the rule outside the controller.
- Extend the `Reimbursement` lifecycle only if new states or transitions are formally required.
- Persist additional approval or audit information only if the policy requires it.
- Add automated tests for boundary values, approval behavior, invalid transitions, and concurrency.

No threshold, currency, role, state, or workflow is assumed beforehand.

## Future Enhancements

- Authentication and authorization.
- Pagination for larger datasets.
- High-value reimbursement validation once the business policy is defined.
- Redis or another distributed cache if distributed idempotency, caching, or cross-instance coordination becomes necessary.
- CI/CD automation.
- Production secret management.
- Extended observability and monitoring.
- Background processing or messaging only if future workflows introduce asynchronous operations.

## Known Limitations

- Swagger is available only in Development.
- The current UI is intentionally simple.
- Production deployment would require external secret management.
