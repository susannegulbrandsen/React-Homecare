import React from 'react';
import { useNavigate } from 'react-router-dom';
import Button from 'react-bootstrap/Button';
import Container from 'react-bootstrap/Container';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import 'bootstrap/dist/css/bootstrap.min.css';

const HomePage: React.FC = () => {
  const navigate = useNavigate();

  const handleAppointmentsClick = () => {
    navigate('/appointments');
  };

  const handleMedicationsClick = () => {
    navigate('/medications');
  };

  const handleInboxClick = () => {
    navigate('/notifications');
  };

  return (
    <div className="text-center py-5 bg-white">
      <h1
        className="fw-bold mb-4"
        style={{
          color: '#212529',
          fontSize: '2.5rem', // litt større for lesbarhet
          letterSpacing: '0.5px',
        }}
      >
        Welcome to Lifelink
      </h1>

      <p
        style={{
          maxWidth: '750px',
          margin: '0 auto 2rem auto',
          color: '#333333',
          fontSize: '1.3rem', // større skrift
          lineHeight: '1.8',  // mer luft mellom linjene
          padding: '0 1rem',
        }}
      >
        At Lifelink, you can easily book appointments for the support you need — whether it’s
        assistance with daily living, medication reminders, shopping, or household chores.
        We’re here to make life at home easier, safer, and more comfortable.
      </p>

      {/* Hovedfunksjoner seksjon */}
      <Container className="py-5">
        <div className="text-center mb-5">
          <h2 
            className="fw-bold mb-4"
            style={{
              color: '#212529',
              fontSize: '2rem',
            }}
          >
            What do you want to do?
          </h2>
        </div>
        
        <Row className="justify-content-center g-4">
          <Col md={4} sm={6} className="d-flex justify-content-center">
            <Button
              variant="primary"
              size="lg"
              onClick={handleAppointmentsClick}
              className="d-flex justify-content-center align-items-center p-4 w-100"
              style={{
                backgroundColor: '#177e8b',
                borderColor: '#177e8b',
                borderRadius: '12px',
                height: '140px',
                maxWidth: '350px',
                minWidth: '280px',
                transition: 'all 0.3s ease',
              }}
              onMouseOver={(e) => {
                e.currentTarget.style.backgroundColor = 'transparent';
                e.currentTarget.style.color = '#177e8b';
              }}
              onMouseOut={(e) => {
                e.currentTarget.style.backgroundColor = '#177e8b';
                e.currentTarget.style.color = 'white';
              }}
            >
              <span style={{ fontSize: '1.4rem', fontWeight: '600', textAlign: 'center' }}>
                Book Appointment
              </span>
            </Button>
          </Col>
          
          <Col md={4} sm={6} className="d-flex justify-content-center">
            <Button
              variant="primary"
              size="lg"
              onClick={handleMedicationsClick}
              className="d-flex justify-content-center align-items-center p-4 w-100"
              style={{
                backgroundColor: '#177e8b',
                borderColor: '#177e8b',
                borderRadius: '12px',
                height: '140px',
                maxWidth: '350px',
                minWidth: '280px',
                transition: 'all 0.3s ease',
              }}
              onMouseOver={(e) => {
                e.currentTarget.style.backgroundColor = 'transparent';
                e.currentTarget.style.color = '#177e8b';
              }}
              onMouseOut={(e) => {
                e.currentTarget.style.backgroundColor = '#177e8b';
                e.currentTarget.style.color = 'white';
              }}
            >
              <span style={{ fontSize: '1.4rem', fontWeight: '600', textAlign: 'center' }}>
                Manage Medications
              </span>
            </Button>
          </Col>
          
          <Col md={4} sm={6} className="d-flex justify-content-center">
            <Button
              variant="primary"
              size="lg"
              onClick={handleInboxClick}
              className="d-flex justify-content-center align-items-center p-4 w-100"
              style={{
                backgroundColor: '#177e8b',
                borderColor: '#177e8b',
                borderRadius: '12px',
                height: '140px',
                maxWidth: '350px',
                minWidth: '280px',
                transition: 'all 0.3s ease',
              }}
              onMouseOver={(e) => {
                e.currentTarget.style.backgroundColor = 'transparent';
                e.currentTarget.style.color = '#177e8b';
              }}
              onMouseOut={(e) => {
                e.currentTarget.style.backgroundColor = '#177e8b';
                e.currentTarget.style.color = 'white';
              }}
            >
              <span style={{ fontSize: '1.4rem', fontWeight: '600', textAlign: 'center' }}>
                Messages
              </span>
            </Button>
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default HomePage;
