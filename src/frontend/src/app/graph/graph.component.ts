import { Component, OnInit } from '@angular/core';
import { GraphService } from './graph.service';

@Component({
  selector: 'app-graph',
  standalone: true,
  imports: [],
  templateUrl: './graph.component.html',
  styleUrl: './graph.component.css'
})
export class GraphComponent implements OnInit {
  constructor(private graphService: GraphService) {}

  ngOnInit(): void {
    this.graphService.init();
  }
}
