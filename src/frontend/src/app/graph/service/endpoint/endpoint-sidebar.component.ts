import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { Endpoint, ServiceData, UseCase } from '../../graph.service';

export interface EndpointSidebarData {
  endpoint: Endpoint;
  service: ServiceData;
  relatedServices: ServiceData[];
  useCases: UseCase[];
}

@Component({
  selector: 'app-endpoint-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDividerModule
  ],
  templateUrl: './endpoint-sidebar.component.html',
  styleUrl: './endpoint-sidebar.component.css'
})
export class EndpointSidebarComponent {
  @Input() data: EndpointSidebarData | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() serviceClick = new EventEmitter<string>();
  @Output() useCaseClick = new EventEmitter<string>();

  onServiceClick(serviceId: string): void {
    this.serviceClick.emit(serviceId);
  }

  onUseCaseClick(useCaseId: string): void {
    this.useCaseClick.emit(useCaseId);
  }
}
