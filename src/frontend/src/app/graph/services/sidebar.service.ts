import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { EndpointSidebarData } from '../components/endpoint-sidebar.component';
import { UseCaseSidebarData } from '../components/usecase-sidebar.component';

@Injectable({
  providedIn: 'root'
})
export class SidebarService {
  private endpointSidebarDataSubject = new BehaviorSubject<EndpointSidebarData | null>(null);
  private endpointSidebarOpenSubject = new BehaviorSubject<boolean>(false);

  private useCaseSidebarDataSubject = new BehaviorSubject<UseCaseSidebarData | null>(null);
  private useCaseSidebarOpenSubject = new BehaviorSubject<boolean>(false);

  public endpointSidebarData$: Observable<EndpointSidebarData | null> = this.endpointSidebarDataSubject.asObservable();
  public endpointSidebarOpen$: Observable<boolean> = this.endpointSidebarOpenSubject.asObservable();

  public useCaseSidebarData$: Observable<UseCaseSidebarData | null> = this.useCaseSidebarDataSubject.asObservable();
  public useCaseSidebarOpen$: Observable<boolean> = this.useCaseSidebarOpenSubject.asObservable();

  openEndpointSidebar(data: EndpointSidebarData): void {
    this.endpointSidebarDataSubject.next(data);
    this.endpointSidebarOpenSubject.next(true);
  }

  closeEndpointSidebar(): void {
    this.endpointSidebarOpenSubject.next(false);
    // Keep data until sidebar animation completes
    setTimeout(() => {
      this.endpointSidebarDataSubject.next(null);
    }, 300);
  }

  openUseCaseSidebar(data: UseCaseSidebarData): void {
    this.useCaseSidebarDataSubject.next(data);
    this.useCaseSidebarOpenSubject.next(true);
  }

  closeUseCaseSidebar(): void {
    this.useCaseSidebarOpenSubject.next(false);
    // Keep data until sidebar animation completes
    setTimeout(() => {
      this.useCaseSidebarDataSubject.next(null);
    }, 300);
  }

  get endpointSidebarOpen(): boolean {
    return this.endpointSidebarOpenSubject.value;
  }

  get useCaseSidebarOpen(): boolean {
    return this.useCaseSidebarOpenSubject.value;
  }
}
