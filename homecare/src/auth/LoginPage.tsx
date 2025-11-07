import React, { useState } from 'react';
import { useAuth } from './AuthContext';
import { useNavigate } from 'react-router-dom';
import { Form, Button, Container, Alert } from 'react-bootstrap';

const LoginPage: React.FC = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const { login } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        try {
            const user = await login({ username, password });
            
            // Check if user has completed their profile
            const token = localStorage.getItem('token');
            let endpoint = '';
            
            if (user.role === 'Patient') {
                endpoint = `/api/patient/user/${user.sub}`;
            } else if (user.role === 'Employee') {
                endpoint = `/api/employee/user/${user.sub}`;
            }
            
            if (endpoint) {
                try {
                    const profileResponse = await fetch(`http://localhost:5090${endpoint}`, {
                        headers: {
                            'Authorization': `Bearer ${token}`,
                            'Content-Type': 'application/json'
                        }
                    });
                    
                    if (profileResponse.status === 404) {
                        // Profile not complete, redirect to profile setup
                        navigate('/profile-setup');
                        return;
                    } else if (profileResponse.ok) {
                        const profileData = await profileResponse.json();
                        // Check if profile has minimal required data
                        const isProfileComplete = profileData.fullName && 
                                                profileData.fullName !== user.username &&
                                                profileData.address;
                        
                        if (!isProfileComplete) {
                            navigate('/profile-setup');
                            return;
                        }
                    }
                } catch (profileErr) {
                    console.error('Error checking profile:', profileErr);
                    // If we can't check profile, redirect to setup to be safe
                    navigate('/profile-setup');
                    return;
                }
            }
            
            // Navigate to appointments if profile is complete
            navigate('/appointments');
            
        } catch (err) {
            setError('Invalid username or password.');
            console.error(err);
        }
    };

    return (
        <Container className="mt-5">
            <h2>Login</h2>
            {error && <Alert variant="danger">{error}</Alert>}
            <Form onSubmit={handleSubmit}>
                <Form.Group className="mb-3" controlId="formBasicUsername">
                    <Form.Label>Username</Form.Label>
                    <Form.Control
                        type="text"
                        placeholder="Enter username"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        required/>
                </Form.Group>

                <Form.Group className="mb-3" controlId="formBasicPassword">
                    <Form.Label>Password</Form.Label>
                    <Form.Control
                        type="password"
                        placeholder="Password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required/>
                </Form.Group>
                <Button variant="primary" type="submit">Login</Button>
            </Form>
        </Container>
    );
};

export default LoginPage;