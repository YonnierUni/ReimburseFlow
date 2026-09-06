import { useCallback, useEffect, useState } from 'react'
import { ApiError, getReimbursements } from '../api/reimbursementsApi'
import type {
  ExpenseCategory,
  Reimbursement,
  ReimbursementStatus,
} from '../models/reimbursement'

type Filters = {
  status?: ReimbursementStatus
  category?: ExpenseCategory
}

export function useReimbursements(filters: Filters) {
  const [items, setItems] = useState<Reimbursement[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [requestVersion, setRequestVersion] = useState(0)

  const refresh = useCallback(async () => {
    setRequestVersion((version) => version + 1)
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    const version = requestVersion
    setLoading(true)
    setError('')

    void getReimbursements(filters, controller.signal)
      .then((nextItems) => {
        if (!controller.signal.aborted) setItems(nextItems)
      })
      .catch((caught) => {
        if (controller.signal.aborted || (caught instanceof DOMException && caught.name === 'AbortError')) return
        setError(caught instanceof ApiError ? caught.message : 'Could not connect to the server.')
      })
      .finally(() => {
        if (!controller.signal.aborted && version === requestVersion) setLoading(false)
      })

    return () => controller.abort()
  }, [filters.category, filters.status, requestVersion])

  return { items, loading, error, refresh }
}
