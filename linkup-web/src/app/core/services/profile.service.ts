import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  UserProfileDto, UpdateProfileDto,
  EducationDto, ExperienceDto, SocialLinkDto, PrivacySettingsDto
} from '../models/profile.model';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private base = `${environment.apiUrl}/profile`;

  constructor(private http: HttpClient) {}

  getProfile(userId: string): Observable<ApiResponse<UserProfileDto>> {
    return this.http.get<ApiResponse<UserProfileDto>>(`${this.base}/${userId}`);
  }

  updateProfile(dto: UpdateProfileDto): Observable<ApiResponse<UserProfileDto>> {
    return this.http.put<ApiResponse<UserProfileDto>>(this.base, dto);
  }

  uploadProfilePicture(file: File): Observable<ApiResponse<UserProfileDto>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<UserProfileDto>>(`${this.base}/picture`, form);
  }

  uploadCoverPhoto(file: File): Observable<ApiResponse<UserProfileDto>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<UserProfileDto>>(`${this.base}/cover`, form);
  }

  addEducation(dto: EducationDto): Observable<ApiResponse<EducationDto>> {
    return this.http.post<ApiResponse<EducationDto>>(`${this.base}/education`, dto);
  }

  updateEducation(id: string, dto: EducationDto): Observable<ApiResponse<EducationDto>> {
    return this.http.put<ApiResponse<EducationDto>>(`${this.base}/education/${id}`, dto);
  }

  deleteEducation(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/education/${id}`);
  }

  addExperience(dto: ExperienceDto): Observable<ApiResponse<ExperienceDto>> {
    return this.http.post<ApiResponse<ExperienceDto>>(`${this.base}/experience`, dto);
  }

  updateExperience(id: string, dto: ExperienceDto): Observable<ApiResponse<ExperienceDto>> {
    return this.http.put<ApiResponse<ExperienceDto>>(`${this.base}/experience/${id}`, dto);
  }

  deleteExperience(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/experience/${id}`);
  }

  getSocialLinks(userId: string): Observable<ApiResponse<SocialLinkDto[]>> {
    return this.http.get<ApiResponse<SocialLinkDto[]>>(`${this.base}/${userId}/social-links`);
  }

  addSocialLink(dto: SocialLinkDto): Observable<ApiResponse<SocialLinkDto>> {
    return this.http.post<ApiResponse<SocialLinkDto>>(`${this.base}/social-links`, dto);
  }

  deleteSocialLink(id: string): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.base}/social-links/${id}`);
  }

  getPrivacySettings(): Observable<ApiResponse<PrivacySettingsDto>> {
    return this.http.get<ApiResponse<PrivacySettingsDto>>(`${this.base}/privacy`);
  }

  updatePrivacySettings(dto: PrivacySettingsDto): Observable<ApiResponse<PrivacySettingsDto>> {
    return this.http.put<ApiResponse<PrivacySettingsDto>>(`${this.base}/privacy`, dto);
  }
}
