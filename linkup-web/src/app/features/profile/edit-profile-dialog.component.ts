import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ProfileService } from '../../core/services/profile.service';
import {
  UserProfileDto, UpdateProfileDto, EducationDto, ExperienceDto, SocialLinkDto
} from '../../core/models/profile.model';

@Component({
  selector: 'app-edit-profile-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatTabsModule, MatCheckboxModule
  ],
  template: `
    <h2 mat-dialog-title>Edit profile</h2>
    <mat-dialog-content class="!pt-2">
      <mat-tab-group>
        <!-- Basic info -->
        <mat-tab label="Basic">
          <form [formGroup]="form" class="space-y-2 pt-4">
            <div class="flex gap-3">
              <mat-form-field class="flex-1" appearance="outline">
                <mat-label>First name</mat-label>
                <input matInput formControlName="firstName">
              </mat-form-field>
              <mat-form-field class="flex-1" appearance="outline">
                <mat-label>Last name</mat-label>
                <input matInput formControlName="lastName">
              </mat-form-field>
            </div>
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Bio</mat-label>
              <textarea matInput rows="3" formControlName="bio"></textarea>
            </mat-form-field>
            <div class="flex gap-3">
              <mat-form-field class="flex-1" appearance="outline">
                <mat-label>Gender</mat-label>
                <mat-select formControlName="gender">
                  <mat-option value="">Unspecified</mat-option>
                  <mat-option value="Male">Male</mat-option>
                  <mat-option value="Female">Female</mat-option>
                  <mat-option value="Other">Other</mat-option>
                </mat-select>
              </mat-form-field>
              <mat-form-field class="flex-1" appearance="outline">
                <mat-label>Birthday</mat-label>
                <input matInput type="date" formControlName="dateOfBirth">
              </mat-form-field>
            </div>
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Location</mat-label>
              <input matInput formControlName="location">
            </mat-form-field>
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Website</mat-label>
              <input matInput formControlName="website">
            </mat-form-field>
            <button mat-flat-button color="primary" (click)="saveBasic()" [disabled]="form.invalid || savingBasic()">
              {{ savingBasic() ? 'Saving...' : 'Save basic info' }}
            </button>
          </form>
        </mat-tab>

        <!-- Education -->
        <mat-tab label="Education">
          <div class="pt-4 space-y-3">
            @for (edu of education(); track edu.id) {
              <div class="flex items-center justify-between bg-gray-50 rounded-lg p-3">
                <div>
                  <p class="font-medium text-gray-800">{{ edu.school }}</p>
                  <p class="text-sm text-gray-500">{{ edu.degree }} {{ edu.fieldOfStudy ? '· ' + edu.fieldOfStudy : '' }}</p>
                </div>
                <button mat-icon-button color="warn" (click)="removeEducation(edu)"><mat-icon>delete</mat-icon></button>
              </div>
            }
            <form [formGroup]="eduForm" class="space-y-2 border-t pt-3">
              <mat-form-field class="w-full" appearance="outline"><mat-label>School</mat-label><input matInput formControlName="school"></mat-form-field>
              <div class="flex gap-3">
                <mat-form-field class="flex-1" appearance="outline"><mat-label>Degree</mat-label><input matInput formControlName="degree"></mat-form-field>
                <mat-form-field class="flex-1" appearance="outline"><mat-label>Field of study</mat-label><input matInput formControlName="fieldOfStudy"></mat-form-field>
              </div>
              <div class="flex gap-3 items-center">
                <mat-form-field class="flex-1" appearance="outline"><mat-label>Start year</mat-label><input matInput type="number" formControlName="startYear"></mat-form-field>
                <mat-form-field class="flex-1" appearance="outline"><mat-label>End year</mat-label><input matInput type="number" formControlName="endYear"></mat-form-field>
              </div>
              <button mat-stroked-button color="primary" (click)="addEducation()" [disabled]="eduForm.invalid">
                <mat-icon>add</mat-icon> Add education
              </button>
            </form>
          </div>
        </mat-tab>

        <!-- Work -->
        <mat-tab label="Work">
          <div class="pt-4 space-y-3">
            @for (exp of experience(); track exp.id) {
              <div class="flex items-center justify-between bg-gray-50 rounded-lg p-3">
                <div>
                  <p class="font-medium text-gray-800">{{ exp.position }} at {{ exp.company }}</p>
                  @if (exp.isCurrent) { <p class="text-sm text-green-600">Current</p> }
                </div>
                <button mat-icon-button color="warn" (click)="removeExperience(exp)"><mat-icon>delete</mat-icon></button>
              </div>
            }
            <form [formGroup]="expForm" class="space-y-2 border-t pt-3">
              <div class="flex gap-3">
                <mat-form-field class="flex-1" appearance="outline"><mat-label>Position</mat-label><input matInput formControlName="position"></mat-form-field>
                <mat-form-field class="flex-1" appearance="outline"><mat-label>Company</mat-label><input matInput formControlName="company"></mat-form-field>
              </div>
              <div class="flex gap-3">
                <mat-form-field class="flex-1" appearance="outline"><mat-label>Start date</mat-label><input matInput type="date" formControlName="startDate"></mat-form-field>
                <mat-form-field class="flex-1" appearance="outline"><mat-label>End date</mat-label><input matInput type="date" formControlName="endDate"></mat-form-field>
              </div>
              <mat-checkbox formControlName="isCurrent">I currently work here</mat-checkbox>
              <div>
                <button mat-stroked-button color="primary" (click)="addExperience()" [disabled]="expForm.invalid">
                  <mat-icon>add</mat-icon> Add work
                </button>
              </div>
            </form>
          </div>
        </mat-tab>

        <!-- Social -->
        <mat-tab label="Social">
          <div class="pt-4 space-y-3">
            @for (link of socialLinks(); track link.id) {
              <div class="flex items-center justify-between bg-gray-50 rounded-lg p-3">
                <div>
                  <p class="font-medium text-gray-800">{{ link.platform }}</p>
                  <a [href]="link.url" target="_blank" class="text-sm text-[#1877f2]">{{ link.url }}</a>
                </div>
                <button mat-icon-button color="warn" (click)="removeSocialLink(link)"><mat-icon>delete</mat-icon></button>
              </div>
            }
            <form [formGroup]="linkForm" class="space-y-2 border-t pt-3">
              <mat-form-field class="w-full" appearance="outline">
                <mat-label>Platform</mat-label>
                <mat-select formControlName="platform">
                  @for (p of platforms; track p) { <mat-option [value]="p">{{ p }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field class="w-full" appearance="outline"><mat-label>URL</mat-label><input matInput formControlName="url"></mat-form-field>
              <button mat-stroked-button color="primary" (click)="addSocialLink()" [disabled]="linkForm.invalid">
                <mat-icon>add</mat-icon> Add link
              </button>
            </form>
          </div>
        </mat-tab>
      </mat-tab-group>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Done</button>
    </mat-dialog-actions>
  `
})
export class EditProfileDialogComponent {
  private fb = inject(FormBuilder);
  private profileSvc = inject(ProfileService);
  private dialogRef = inject(MatDialogRef<EditProfileDialogComponent>);
  data = inject<{ profile: UserProfileDto }>(MAT_DIALOG_DATA);

  private changed = false;
  platforms = ['Website', 'Twitter', 'LinkedIn', 'GitHub', 'Instagram', 'YouTube'];

  form = this.fb.group({
    firstName: [this.data.profile.firstName, Validators.required],
    lastName: [this.data.profile.lastName, Validators.required],
    bio: [this.data.profile.bio ?? ''],
    gender: [this.data.profile.gender ?? ''],
    dateOfBirth: [this.data.profile.dateOfBirth ? this.data.profile.dateOfBirth.substring(0, 10) : ''],
    location: [this.data.profile.location ?? ''],
    website: [this.data.profile.website ?? '']
  });
  savingBasic = signal(false);

  education = signal<EducationDto[]>([...(this.data.profile.education ?? [])]);
  experience = signal<ExperienceDto[]>([...(this.data.profile.experience ?? [])]);
  socialLinks = signal<SocialLinkDto[]>([...(this.data.profile.socialLinks ?? [])]);

  eduForm = this.fb.group({
    school: ['', Validators.required], degree: [''], fieldOfStudy: [''],
    startYear: [new Date().getFullYear()], endYear: [null as number | null]
  });
  expForm = this.fb.group({
    position: ['', Validators.required], company: ['', Validators.required],
    startDate: [''], endDate: [''], isCurrent: [false]
  });
  linkForm = this.fb.group({ platform: ['Website', Validators.required], url: ['', Validators.required] });

  saveBasic(): void {
    if (this.form.invalid) return;
    this.savingBasic.set(true);
    const dto = this.form.value as UpdateProfileDto;
    this.profileSvc.updateProfile(dto).subscribe({
      next: () => { this.savingBasic.set(false); this.changed = true; },
      error: () => this.savingBasic.set(false)
    });
  }

  addEducation(): void {
    if (this.eduForm.invalid) return;
    this.profileSvc.addEducation(this.eduForm.value as EducationDto).subscribe({
      next: r => { if (r.success) { this.education.update(e => [...e, r.data]); this.changed = true; this.eduForm.reset({ startYear: new Date().getFullYear() }); } }
    });
  }
  removeEducation(edu: EducationDto): void {
    if (!edu.id) return;
    this.profileSvc.deleteEducation(edu.id).subscribe({ next: () => { this.education.update(e => e.filter(x => x.id !== edu.id)); this.changed = true; } });
  }

  addExperience(): void {
    if (this.expForm.invalid) return;
    this.profileSvc.addExperience(this.expForm.value as ExperienceDto).subscribe({
      next: r => { if (r.success) { this.experience.update(e => [...e, r.data]); this.changed = true; this.expForm.reset({ isCurrent: false }); } }
    });
  }
  removeExperience(exp: ExperienceDto): void {
    if (!exp.id) return;
    this.profileSvc.deleteExperience(exp.id).subscribe({ next: () => { this.experience.update(e => e.filter(x => x.id !== exp.id)); this.changed = true; } });
  }

  addSocialLink(): void {
    if (this.linkForm.invalid) return;
    this.profileSvc.addSocialLink(this.linkForm.value as SocialLinkDto).subscribe({
      next: r => { if (r.success) { this.socialLinks.update(l => [...l, r.data]); this.changed = true; this.linkForm.reset({ platform: 'Website' }); } }
    });
  }
  removeSocialLink(link: SocialLinkDto): void {
    if (!link.id) return;
    this.profileSvc.deleteSocialLink(link.id).subscribe({ next: () => { this.socialLinks.update(l => l.filter(x => x.id !== link.id)); this.changed = true; } });
  }

  close(): void { this.dialogRef.close(this.changed); }
}
