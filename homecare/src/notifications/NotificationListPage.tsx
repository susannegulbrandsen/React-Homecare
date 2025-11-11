import React, { useState, useEffect } from 'react';
import { Container, Card, Row, Col, Button, Alert, Spinner, Badge, ButtonGroup } from 'react-bootstrap';
import { useAuth } from '../auth/AuthContext';
import { NotificationService } from './NotificationService';
import type { NotificationDto } from '../types/notification';
import { useNavigate } from 'react-router-dom';

const NotificationListPage: React.FC = () => {
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'unread'>('all');
  const { user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!user) {
      navigate('/login');
      return;
    }
    loadNotifications();
  }, [user, filter, navigate]);

  const loadNotifications = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const data = filter === 'unread' 
        ? await NotificationService.getUnreadNotifications()
        : await NotificationService.getMyNotifications();
      
      setNotifications(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load notifications');
      console.error('Error loading notifications:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleMarkAsRead = async (notificationId: number) => {
    try {
      await NotificationService.markAsRead(notificationId);
      // Update the notification in the list
      setNotifications(prev => 
        prev.map(n => 
          n.notificationId === notificationId 
            ? { ...n, isRead: true }
            : n
        )
      );
    } catch (err) {
      console.error('Error marking notification as read:', err);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await NotificationService.markAllAsRead();
      // Update all notifications to read
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch (err) {
      console.error('Error marking all notifications as read:', err);
    }
  };

  const handleDeleteNotification = async (notificationId: number) => {
    try {
      await NotificationService.deleteNotification(notificationId);
      // Remove notification from list
      setNotifications(prev => prev.filter(n => n.notificationId !== notificationId));
    } catch (err) {
      console.error('Error deleting notification:', err);
    }
  };

  const handleNotificationClick = (notification: NotificationDto) => {
    // Mark as read when clicked
    if (!notification.isRead) {
      handleMarkAsRead(notification.notificationId!);
    }

    // Navigate to related item if applicable
    if (notification.type === 'appointment' && notification.relatedId) {
      navigate('/appointments');
    } else if (notification.type === 'medication' && notification.relatedId) {
      navigate('/medications');
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - date.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 1) {
      return 'Today';
    } else if (diffDays === 2) {
      return 'Yesterday';
    } else if (diffDays <= 7) {
      return `${diffDays - 1} days ago`;
    } else {
      return date.toLocaleDateString();
    }
  };

  const unreadCount = notifications.filter(n => !n.isRead).length;

  if (!user) {
    return (
      <Container className="mt-5">
        <Alert variant="warning">
          Please log in to view your notifications.
        </Alert>
      </Container>
    );
  }

  return (
    <Container className="mt-4">
      <Row>
        <Col>
          <div className="d-flex justify-content-between align-items-center mb-4">
            <h2 style={{ color: '#177e8b' }}>
              Notifications 
              {unreadCount > 0 && (
                <Badge bg="danger" className="ms-2">
                  {unreadCount}
                </Badge>
              )}
            </h2>
            <div className="d-flex gap-2">
              <ButtonGroup>
                <Button 
                  variant={filter === 'all' ? 'primary' : 'outline-primary'}
                  onClick={() => setFilter('all')}
                  style={{ 
                    backgroundColor: filter === 'all' ? '#177e8b' : 'transparent',
                    borderColor: '#177e8b',
                    color: filter === 'all' ? 'white' : '#177e8b'
                  }}
                >
                  All
                </Button>
                <Button 
                  variant={filter === 'unread' ? 'primary' : 'outline-primary'}
                  onClick={() => setFilter('unread')}
                  style={{ 
                    backgroundColor: filter === 'unread' ? '#177e8b' : 'transparent',
                    borderColor: '#177e8b',
                    color: filter === 'unread' ? 'white' : '#177e8b'
                  }}
                >
                  Unread
                </Button>
              </ButtonGroup>
              {unreadCount > 0 && (
                <Button 
                  variant="outline-secondary" 
                  onClick={handleMarkAllAsRead}
                  style={{ fontSize: '0.9rem' }}
                >
                  Mark All as Read
                </Button>
              )}
            </div>
          </div>

          {error && (
            <Alert variant="danger">
              {error}
            </Alert>
          )}

          {loading ? (
            <div className="text-center py-5">
              <Spinner animation="border" role="status" style={{ color: '#177e8b' }}>
                <span className="visually-hidden">Loading...</span>
              </Spinner>
            </div>
          ) : notifications.length === 0 ? (
            <Card className="text-center py-5">
              <Card.Body>
                <h5 className="text-muted">No notifications found</h5>
                <p className="text-muted">
                  {filter === 'unread' 
                    ? "You have no unread notifications." 
                    : "You haven't received any notifications yet."
                  }
                </p>
              </Card.Body>
            </Card>
          ) : (
            <div>
              {notifications.map((notification) => (
                <Card 
                  key={notification.notificationId} 
                  className={`mb-3 ${!notification.isRead ? 'border-primary' : ''}`}
                  style={{ 
                    cursor: 'pointer',
                    backgroundColor: !notification.isRead ? '#f8f9ff' : 'white'
                  }}
                  onClick={() => handleNotificationClick(notification)}
                >
                  <Card.Body>
                    <Row>
                      <Col xs={10}>
                        <div className="d-flex justify-content-between align-items-start mb-2">
                          <h6 className="mb-1" style={{ 
                            fontWeight: !notification.isRead ? 'bold' : 'normal',
                            color: '#177e8b'
                          }}>
                            {notification.title}
                            {!notification.isRead && (
                              <Badge bg="primary" className="ms-2" style={{ fontSize: '0.7rem' }}>
                                New
                              </Badge>
                            )}
                          </h6>
                          <small className="text-muted">
                            {formatDate(notification.createdAt)}
                          </small>
                        </div>
                        <p className="mb-0" style={{ 
                          fontWeight: !notification.isRead ? '500' : 'normal',
                          fontSize: '1.1rem'
                        }}>
                          {notification.message}
                        </p>
                        {notification.type && (
                          <Badge 
                            bg="secondary" 
                            className="mt-2"
                            style={{ fontSize: '0.8rem' }}
                          >
                            {notification.type}
                          </Badge>
                        )}
                      </Col>
                      <Col xs={2} className="text-end">
                        <div className="d-flex flex-column gap-1">
                          {!notification.isRead && (
                            <Button
                              size="sm"
                              variant="outline-success"
                              onClick={(e) => {
                                e.stopPropagation();
                                handleMarkAsRead(notification.notificationId!);
                              }}
                              style={{ fontSize: '0.8rem' }}
                            >
                              Read
                            </Button>
                          )}
                          <Button
                            size="sm"
                            variant="outline-danger"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDeleteNotification(notification.notificationId!);
                            }}
                            style={{ fontSize: '0.8rem' }}
                          >
                            Delete
                          </Button>
                        </div>
                      </Col>
                    </Row>
                  </Card.Body>
                </Card>
              ))}
            </div>
          )}
        </Col>
      </Row>
    </Container>
  );
};

export default NotificationListPage;