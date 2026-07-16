import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  async submit(): Promise<void> {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      await this.auth.login(this.form.getRawValue());
      const returnUrl = this.sanitizeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
      if (returnUrl) {
        await this.router.navigateByUrl(returnUrl);
      } else if (this.auth.isAdmin()) {
        await this.router.navigate(['/admin']);
      } else {
        await this.router.navigate(['/dashboard']);
      }
    } catch {
      this.errorMessage.set('Invalid email or password. Please try again.');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private sanitizeReturnUrl(returnUrl: string | null): string | null {
    if (!returnUrl) {
      return null;
    }

    if (!returnUrl.startsWith('/') || returnUrl.startsWith('//')) {
      return null;
    }

    if (returnUrl === '/login' || returnUrl === '/register' || returnUrl.startsWith('/login?') || returnUrl.startsWith('/register?')) {
      return null;
    }

    return returnUrl;
  }
}
