import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CompanyApiService } from '../../core/api/company-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Company, CompanyMember, CompanyMine } from '../../core/models/api.models';

@Component({
  selector: 'app-company',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './company.component.html',
  styleUrl: './company.component.scss',
})
export class CompanyComponent implements OnInit {
  private readonly companyApi = inject(CompanyApiService);
  private readonly formBuilder = inject(FormBuilder);
  protected readonly auth = inject(AuthService);

  protected readonly isLoading = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly mine = signal<CompanyMine | null>(null);
  protected readonly members = signal<CompanyMember[]>([]);
  protected readonly allCompanies = signal<Company[]>([]);

  protected readonly joinForm = this.formBuilder.nonNullable.group({
    inviteCode: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(32)]],
  });

  protected readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    slug: ['', [Validators.maxLength(64), Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)]],
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  /** JWT role claims — Members / Rotate require these; GetMine.isCompanyAdmin is DB-only. */
  protected canManageCompany(): boolean {
    return this.auth.isCompanyAdmin() || this.auth.isAdmin();
  }

  /** DB says CompanyAdmin but JWT not yet refreshed (Join token fail-soft). */
  protected needsCompanyAdminRelogin(): boolean {
    return !!this.mine()?.isCompanyAdmin && !this.canManageCompany();
  }

  async reload(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const mine = await this.companyApi.getMine();
      this.mine.set(mine);

      // Gate on JWT roles, not GetMine.isCompanyAdmin — Join may persist CompanyAdmin
      // while token re-issue is skipped/fails; Members/Rotate authorize on claims.
      if (mine.company != null && this.canManageCompany()) {
        try {
          const members = await this.companyApi.getMembers(
            this.auth.isAdmin() && !this.auth.isCompanyAdmin() ? mine.company.id : undefined,
          );
          this.members.set(members);
        } catch {
          this.members.set([]);
        }
      } else {
        this.members.set([]);
      }

      if (this.auth.isAdmin()) {
        this.allCompanies.set(await this.companyApi.getAll());
      } else {
        this.allCompanies.set([]);
      }
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Failed to load company details.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  async join(): Promise<void> {
    if (this.joinForm.invalid || this.isSubmitting()) {
      this.joinForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const result = await this.companyApi.join({
        inviteCode: this.joinForm.controls.inviteCode.value.trim(),
      });

      if (result.token) {
        const refreshed = await this.auth.applyAccessToken(result.token);
        if (!refreshed && result.becameCompanyAdmin) {
          this.joinForm.reset();
          this.successMessage.set(
            `Joined ${result.company.name} as company admin, but session refresh failed. Log out and log in again to manage invites.`,
          );
          await this.reload();
          return;
        }
      } else if (result.becameCompanyAdmin) {
        this.joinForm.reset();
        this.successMessage.set(
          `Joined ${result.company.name} as company admin. Log out and log in again so your admin role is active.`,
        );
        await this.reload();
        return;
      }

      this.joinForm.reset();
      this.successMessage.set(
        result.becameCompanyAdmin
          ? `Joined ${result.company.name}. You are the company admin.`
          : `Joined ${result.company.name}.`,
      );
      await this.reload();
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Could not join with that invite code.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async rotateInvite(): Promise<void> {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const companyId =
        this.auth.isAdmin() && !this.auth.isCompanyAdmin()
          ? this.mine()?.company?.id
          : undefined;
      const company = await this.companyApi.rotateInviteCode(companyId);
      this.successMessage.set(`Invite code rotated to ${company.inviteCode}.`);
      await this.reload();
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Could not rotate invite code.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async createCompany(): Promise<void> {
    if (this.createForm.invalid || this.isSubmitting()) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      const raw = this.createForm.getRawValue();
      const slug = raw.slug.trim();
      const company = await this.companyApi.create({
        name: raw.name.trim(),
        slug: slug.length > 0 ? slug : null,
      });
      this.createForm.reset();
      this.successMessage.set(
        `Created ${company.name}. Invite code: ${company.inviteCode ?? '—'}.`,
      );
      await this.reload();
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Could not create company.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private readApiError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error;
      if (typeof body === 'object' && body !== null && 'error' in body) {
        const message = (body as { error?: string }).error;
        if (typeof message === 'string' && message.trim().length > 0) {
          return message;
        }
      }
    }

    return fallback;
  }
}
