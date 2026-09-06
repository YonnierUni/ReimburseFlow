import type { ReimbursementStatus } from '../models/reimbursement'

type StatusBadgeProps = { status: ReimbursementStatus }

export function StatusBadge({ status }: StatusBadgeProps) {
  return <span className={`status-badge status-${status.toLowerCase()}`}>{status}</span>
}
