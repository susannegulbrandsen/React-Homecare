import React, { useState, useEffect } from 'react';
import { Nav, Navbar, Button, Form, InputGroup, Badge } from 'react-bootstrap';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { NotificationService } from '../notifications/NotificationService';

const NavMenu: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [unreadCount, setUnreadCount] = useState<number>(0);

  // Load unread notification count
  useEffect(() => {
    if (user) {
      loadUnreadCount();
      // Refresh count every 30 seconds
      const interval = setInterval(loadUnreadCount, 30000);
      return () => clearInterval(interval);
    }
  }, [user]);

  const loadUnreadCount = async () => {
    try {
      const count = await NotificationService.getUnreadCount();
      setUnreadCount(count);
    } catch (err) {
      console.error('Error loading unread count:', err);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchQuery.trim()) return;
    
    // Navigate to a search results page with the query
    navigate(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
    setSearchQuery(''); // Clear search after navigation
  };

  const handleNotificationClick = () => {
    navigate('/notifications');
  };

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <div style={{ 
      backgroundColor: '#f8f9fa', 
      borderBottom: '1px solid #dee2e6', 
      width: '100vw',
      marginLeft: 'calc(-50vw + 50%)',
      marginRight: 'calc(-50vw + 50%)',
      padding: '1rem 0'
    }}>
      <div style={{ maxWidth: '1400px', margin: '0 auto', padding: '0 1.5rem' }}>
        <Navbar expand="lg" style={{ backgroundColor: 'transparent', minHeight: '70px' }} className="px-0 d-flex align-items-center">
          {/* Logo */}
          <Navbar.Brand 
            as={Link} 
            to="/" 
            className="fw-bold d-flex align-items-center" 
            style={{ 
              color: '#177e8b', 
              fontSize: '1.8rem',
              padding: '0.5rem 0'
            }}
          >
            Lifelink
          </Navbar.Brand>

      {/* Right side content for mobile - shown before hamburger */}
      <div className="d-flex d-lg-none align-items-center" style={{ height: '70px' }}>
        {user ? (
          <>
            {/* Notifications for mobile */}
            <Button
              variant="outline-secondary"
              onClick={handleNotificationClick}
              className="position-relative me-2"
              style={{ 
                border: 'none',
                fontSize: '1.4rem',
                padding: '0.75rem 1rem'
              }}
            >
              <i className="bi bi-bell" style={{ fontSize: '1.2rem' }}></i>
              {unreadCount > 0 && (
                <Badge 
                  bg="danger" 
                  className="position-absolute top-0 start-100 translate-middle"
                  style={{ fontSize: '0.8rem' }}
                >
                  {unreadCount}
                </Badge>
              )}
            </Button>
          </>
        ) : null}
      </div>

      {/* Hamburger Menu */}
      <Navbar.Toggle aria-controls="basic-navbar-nav" />
      <Navbar.Collapse id="basic-navbar-nav">
        <Nav className="me-auto">
          <Nav.Link 
            as={Link} 
            to="/" 
            style={{ 
              fontSize: '1.2rem', 
              padding: '0.75rem 1rem',
              fontWeight: '500'
            }}
          >
            Home
          </Nav.Link>
          
          {/* Profile link for all authenticated users */}
          {user && <Nav.Link as={Link} to="/profile" style={{ fontSize: '1.1rem', padding: '0.75rem 1rem' }}>My Profile</Nav.Link>}
          
          {/* Employee-specific navigation */}
          {user?.role === 'Employee' && (
            <>
              <Nav.Link as={Link} to="/appointments" style={{ fontSize: '1.1rem', padding: '0.75rem 1rem' }}>Appointments</Nav.Link>
              <Nav.Link as={Link} to="/medications" style={{ fontSize: '1.1rem', padding: '0.75rem 1rem' }}>Medications</Nav.Link>
            </>
          )}
          
          {/* Patient-specific navigation */}
          {user?.role === 'Patient' && (
            <>
              <Nav.Link as={Link} to="/appointments" style={{ fontSize: '1.1rem', padding: '0.75rem 1rem' }}>Appointments</Nav.Link>
              <Nav.Link as={Link} to="/medications" style={{ fontSize: '1.1rem', padding: '0.75rem 1rem' }}>Medications</Nav.Link>
            </>
          )}

          {/* Mobile-only items */}
          <div className="d-lg-none">
            {user && (
              <>
                <hr className="my-2" />
                <Nav.Link
                  as={Link}
                  to="/profile"
                  style={{ 
                    color: '#177e8b',
                    fontSize: '1.1rem',
                    padding: '0.75rem 0'
                  }}
                >
                  Welcome, {user.username}
                </Nav.Link>
                <Nav.Link 
                  onClick={handleLogout} 
                  style={{ 
                    color: '#dc3545',
                    fontSize: '1.1rem',
                    padding: '0.75rem 0'
                  }}
                >
                  Log out
                </Nav.Link>
              </>
            )}
            {!user && (
              <>
                <hr className="my-2" />
                <Nav.Link 
                  as={Link} 
                  to="/login" 
                  style={{ 
                    color: '#177e8b',
                    fontSize: '1.1rem',
                    padding: '0.75rem 0'
                  }}
                >
                  Login
                </Nav.Link>
                <Nav.Link 
                  as={Link} 
                  to="/register" 
                  style={{ 
                    color: '#177e8b',
                    fontSize: '1.1rem',
                    padding: '0.75rem 0'
                  }}
                >
                  Register
                </Nav.Link>
              </>
            )}
          </div>
        </Nav>

        {/* Right side content for desktop */}
        <div className="d-none d-lg-flex align-items-center gap-4" style={{ height: '70px' }}>
          {user ? (
            // Logged in state
            <>
              {/* Search bar */}
              <Form onSubmit={handleSearch} className="d-none d-md-block">
                <InputGroup style={{ width: '280px' }}>
                  <Form.Control
                    type="search"
                    placeholder="Search..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    style={{ fontSize: '1.1rem', padding: '0.75rem' }}
                  />
                  <Button 
                    variant="outline-secondary" 
                    type="submit"
                    style={{ 
                      fontSize: '1.1rem', 
                      padding: '0.75rem 1rem',
                      color: '#333'
                    }}
                  >
                    ⌕
                  </Button>
                </InputGroup>
              </Form>

              {/* Notifications */}
              <Button
                variant="outline-secondary"
                onClick={handleNotificationClick}
                className="position-relative"
                style={{ 
                  border: 'none',
                  fontSize: '1.4rem',
                  padding: '0.75rem 1rem'
                }}
              >
                <i className="bi bi-bell" style={{ fontSize: '1.2rem' }}></i>
                {unreadCount > 0 && (
                  <Badge 
                    bg="danger" 
                    className="position-absolute top-0 start-100 translate-middle"
                    style={{ fontSize: '0.8rem' }}
                  >
                    {unreadCount}
                  </Badge>
                )}
              </Button>

              {/* Welcome message - clickable */}
              <Link 
                to="/profile" 
                className="text-decoration-none d-none d-lg-block"
                style={{ color: '#177e8b' }}
              >
                <span className="text-muted" style={{ cursor: 'pointer' }}>
                  Welcome, {user.username}
                </span>
              </Link>

              {/* Logout button */}
              <Button 
                variant="outline-danger" 
                onClick={handleLogout}
                style={{
                  fontSize: '1.1rem',
                  padding: '0.75rem 1.5rem'
                }}
              >
                Log out
              </Button>
            </>
          ) : (
            // Not logged in state
            <>
              <Link to="/login" className="me-3">
                <Button 
                  variant="outline-primary"
                  style={{ 
                    borderColor: '#177e8b', 
                    color: '#177e8b',
                    backgroundColor: 'transparent',
                    transition: 'all 0.3s ease',
                    fontSize: '1.1rem',
                    padding: '0.75rem 1.5rem'
                  }}
                  onMouseOver={(e) => {
                    e.currentTarget.style.backgroundColor = '#177e8b';
                    e.currentTarget.style.color = 'white';
                  }}
                  onMouseOut={(e) => {
                    e.currentTarget.style.backgroundColor = 'transparent';
                    e.currentTarget.style.color = '#177e8b';
                  }}
                >
                  Login
                </Button>
              </Link>
              <Link to="/register">
                <Button 
                  variant="outline-primary"
                  size="lg"
                  style={{ 
                    borderColor: '#177e8b', 
                    color: '#177e8b',
                    backgroundColor: 'transparent',
                    transition: 'all 0.3s ease',
                    fontSize: '1.1rem',
                    padding: '0.75rem 1.5rem'
                  }}
                  onMouseOver={(e) => {
                    e.currentTarget.style.backgroundColor = '#177e8b';
                    e.currentTarget.style.color = 'white';
                  }}
                  onMouseOut={(e) => {
                    e.currentTarget.style.backgroundColor = 'transparent';
                    e.currentTarget.style.color = '#177e8b';
                  }}
                >
                  Register
                </Button>
              </Link>
            </>
          )}
        </div>
      </Navbar.Collapse>
      </Navbar>
      </div>
    </div>
  );
};

export default NavMenu;