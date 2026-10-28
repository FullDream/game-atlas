import type { FieldTree } from '@angular/forms/signals'
import { pickBy } from 'remeda'

export function getDirtyValues<T extends Record<string, unknown>>(fields: FieldTree<T>): Partial<T> {
	return pickBy(fields().value(), (value, key) => fields[key]?.().dirty() ?? false) as Partial<T>
}
