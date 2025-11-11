export interface Notification {
    notificationId?: number;
    userId: string;
    title: string;
    message: string;
    type?: string; // "appointment", "medication", "general"
    relatedId?: number; // ID of related entity (appointment, medication, etc.)
    isRead: boolean;
    createdAt: string;
}

export interface NotificationDto {
    notificationId?: number;
    userId: string;
    title: string;
    message: string;
    type?: string;
    relatedId?: number;
    isRead: boolean;
    createdAt: string;
}

export interface CreateNotificationDto {
    userId: string;
    title: string;
    message: string;
    type?: string;
    relatedId?: number;
}