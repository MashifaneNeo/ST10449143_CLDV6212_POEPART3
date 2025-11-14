# CLDV6212 POE PART 3
ABC Retail:
Project Overview
ABC Retail is a comprehensive ASP.NET Core e-commerce web application designed to provide a seamless shopping experience for customers while offering robust management capabilities for administrators. This platform features a modern, responsive interface and implements secure authentication, shopping cart functionality and order management systems.

Features:

Customer Features
User Registration & Authentication: Secure account creation and login system with role-based access

Product Catalog: Browse and search through available products with detailed descriptions and images

Shopping Cart: Add items to cart, manage quantities, and proceed to secure checkout

Order Management: View order history and track order status in real-time

File Upload: Submit proof of payment documents for order verification

Administrator Features:
Customer Management: Comprehensive customer database with search and edit capabilities

Product Management: Full CRUD operations for product inventory with image upload support

Order Administration: View and manage all customer orders with status tracking

Dashboard Analytics: Overview of key business metrics including customer count, product inventory, and order statistics

Technical Architecture:
Backend Technologies:
ASP.NET Core MVC: Modern web framework following the Model-View-Controller pattern

Entity Framework Core: Object-relational mapper for database operations

SQL Server: Robust relational database management system

Azure Functions: Serverless computing for API endpoints and business logic

Azure Blob Storage: Cloud storage solution for file uploads and product images

Frontend Technologies
Bootstrap 5: Responsive frontend framework for mobile-first design

Font Awesome: Comprehensive icon library for enhanced user interface

JavaScript ES6: Client-side scripting for dynamic user interactions

Razor Pages: Server-side rendering with C# integration

Security Features
PBKDF2 Password Hashing: Secure password storage using industry-standard algorithms

Session Management: Secure user session handling with timeout protection

Anti-Forgery Tokens: Protection against cross-site request forgery attacks

Role-Based Authorization: Granular access control for different user types

Input Validation: Comprehensive server-side and client-side validation

Key Components
Authentication System:
The application implements a secure authentication system using session-based management. Users can register as either customers or administrators, with each role having distinct permissions and access levels. The system includes secure password hashing and session timeout features.

Shopping Cart Functionality:
The shopping cart system provides customers with a seamless shopping experience. It includes real-time quantity updates, stock validation, and automatic price calculations. The cart persists between sessions and includes tax calculations and order summary previews.

Order Processing:
Orders are processed through a multi-step validation system that checks stock availability before confirmation. The system supports multiple order statuses and provides both customers and administrators with comprehensive order tracking capabilities.

File Management:
The platform includes secure file upload functionality for proof of payment documents. Files are stored in Azure Blob Storage with proper validation for file types and sizes, ensuring secure document handling.

Database Design:
The application uses a relational database design with tables for users, products, customers, orders, and cart items. The schema supports complex relationships while maintaining data integrity through foreign key constraints and proper indexing.

For technical support or questions regarding this application, please contact the development team through the appropriate channels. Include detailed information about any issues encountered.

Youtube video link: https://youtu.be/6NYuDO0gqGk
web app link: https://st10449143-hue5dydfgncgftbg.southafricanorth-01.azurewebsites.net/
