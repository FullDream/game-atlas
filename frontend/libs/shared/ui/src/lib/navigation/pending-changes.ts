import { Service } from '@angular/core'
import { Observable, of } from 'rxjs'

/** Route-scoped bridge between a form and navigation guards. Owns no form data. */
@Service()
export class PendingChanges {
	check: () => Observable<boolean> = () => of(true)

	canLeave(): Observable<boolean> {
		return this.check()
	}
}
