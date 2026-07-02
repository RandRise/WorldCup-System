import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

interface PlaceholderData {
  title: string;
  description: string;
  phaseTask?: string;
}

@Component({
  selector: 'app-placeholder-page',
  templateUrl: './placeholder-page.component.html',
  styleUrl: './placeholder-page.component.scss',
})
export class PlaceholderPageComponent {
  private readonly route = inject(ActivatedRoute);

  protected readonly page = toSignal(
    this.route.data.pipe(
      map((data) => data as PlaceholderData),
    ),
    { initialValue: { title: '', description: '', phaseTask: undefined } satisfies PlaceholderData },
  );
}
