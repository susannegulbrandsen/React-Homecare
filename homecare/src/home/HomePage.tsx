import React from 'react';
import Carousel from 'react-bootstrap/Carousel';
import Image from 'react-bootstrap/Image';
import 'bootstrap/dist/css/bootstrap.min.css';

const HomePage: React.FC = () => {
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

      <div
        className="mx-auto"
        style={{
          maxWidth: '800px',
          position: 'relative',
        }}
      >
        <Carousel fade indicators={false} interval={4000}>
          <Carousel.Item style={{ height: '400px' }}>
            <Image
              src="/images/homevisit.jpg"
              alt="Home Visits"
              fluid
              style={{
                borderRadius: '20px',
                objectFit: 'cover',
                width: '100%',
                height: '100%',
              }}
            />
          </Carousel.Item>
          <Carousel.Item style={{ height: '400px' }}>
            <Image
              src="/images/stetscope.jpeg"
              alt="Experienced Personnel"
              fluid
              style={{
                borderRadius: '20px',
                objectFit: 'cover',
                width: '100%',
                height: '100%',
              }}
            />
          </Carousel.Item>
          <Carousel.Item style={{ height: '400px' }}>
            <Image
              src="/images/sickperson.jpeg"
              alt="Medical Advice"
              fluid
              style={{
                borderRadius: '20px',
                objectFit: 'cover',
                width: '100%',
                height: '100%',
              }}
            />
          </Carousel.Item>
        </Carousel>
      </div>
    </div>
  );
};

export default HomePage;
