import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import './LoginPage.css';
import {useSignIn} from '@/hooks/users/useSignIn'

const LoginPage: React.FC = () => {
  const signIn = useSignIn();


  const [formData, setFormData] = useState({
    email: 'admin',
    password: 'admin',
  });
  const [errors, setErrors] = useState({
    email: '',
    password: '',
  });

  const validateEmail = (email: string) => {
    const emailOrLoginRegex =
    /^(?:[^\s@]+@[^\s@]+\.[^\s@]+|[a-zA-Z0-9._-]{3,})$/;
    return emailOrLoginRegex.test(email);
  };

  const validatePassword = (password: string) => {
    return password.length >= 3;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value,
    }));

    // Clear error when user starts typing
    if (errors[name as keyof typeof errors]) {
      setErrors(prev => ({
        ...prev,
        [name]: '',
      }));
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    let newErrors = { email: '', password: '' };

    if (!validateEmail(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }

    if (!validatePassword(formData.password)) {
      newErrors.password = 'Password must be at least 6 characters long';
    }

    setErrors(newErrors);

    if (!newErrors.email && !newErrors.password) {
      try {
        console.log('Login attempt:', formData);


        signIn.mutate(formData, {
          onSuccess: () => {
            console.log("success");
            window.location.href = '/';
          },
          onError: () => {
            console.error('Failed to update item');
          }
        });
      } catch (error) {
        console.error('Failed to update item');
      }
  };
}

  return (
    <div className="login-page">
      <div className="login-container">
        <h2>Login</h2>
        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="text"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              className={errors.email ? 'error' : ''}
              placeholder="Enter your login or email"
            />
            {errors.email && <span className="error-message">{errors.email}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              className={errors.password ? 'error' : ''}
              placeholder="Enter your password"
            />
            {errors.password && <span className="error-message">{errors.password}</span>}
          </div>

          <button type="submit" className="login-btn">Login</button>
        </form>

        <div className="register-link">
          <p>Don't have an account? <Link to="/register">Register here</Link></p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
