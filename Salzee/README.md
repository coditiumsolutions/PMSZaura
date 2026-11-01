# Juba Smart City - Property Management System (PMS)

A comprehensive Property Management System built with ASP.NET Core MVC for managing real estate projects, customers, properties, and payments for Juba Smart City development.

## Features

### 🏢 Core Modules
- **User Management**: Role-based authentication and authorization
- **Customer Management**: Complete customer lifecycle management
- **Project Management**: Multi-phase project tracking
- **Property Management**: Plot and apartment management with allotment system
- **Payment Management**: Flexible payment plans and installment tracking
- **Transfer Management**: Property ownership transfers
- **Document Management**: File attachments and document storage
- **Activity Logging**: Complete audit trail

### 🎯 Key Features
- Modern, responsive web interface
- Role-based access control (Admin, Manager, Staff)
- Real-time dashboard with statistics
- Payment scheduling and tracking
- Property allotment workflow
- Balloting system for fair property distribution
- Penalty and waiver management
- No Dues Certificate (NDC) generation
- Comprehensive reporting

## Technology Stack

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server with Entity Framework Core
- **Frontend**: Bootstrap 5, jQuery, DataTables
- **Authentication**: Cookie-based authentication with BCrypt password hashing
- **Icons**: Font Awesome

## Database Schema

The system includes the following main entities:

### Users & Access Control
- `ACL` - Role definitions and permissions
- `Users` - User accounts with role assignments
- `UserSessions` - Login session tracking
- `ActivityLog` - User activity audit trail
- `Notifications` - System notifications

### Customer Management
- `Registration` - Initial customer registrations
- `Customers` - Detailed customer profiles
- `CustomerLogs` - Customer activity tracking

### Project & Property Management
- `Projects` - Real estate project definitions
- `Property` - Individual properties (plots/apartments)
- `Allotment` - Property assignments to customers
- `Balloting` - Fair distribution system
- `Possession` - Property possession tracking

### Payment System
- `PaymentPlan` - Flexible payment plan definitions
- `PaymentSchedule` - Individual installment schedules
- `Payments` - Payment records and tracking
- `Penalties` - Late payment penalties
- `Waiver` - Approved payment waivers
- `Refund` - Refund processing

### Additional Features
- `Transfer` - Property ownership transfers
- `NDC` - No Dues Certificates
- `Approval` - Generic approval workflow
- `Attachments` - Document management

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (Local or Remote)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PMS
   ```

2. **Update connection string**
   Edit `appsettings.json` and update the connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=PMS;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
     }
   }
   ```

3. **Restore packages**
   ```bash
   dotnet restore
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   - Open browser and navigate to `https://localhost:5001`
   - Default admin credentials:
     - Email: `admin@jubasmartcity.com`
     - Password: `Admin@123`

### Database Setup

The application will automatically:
- Create the database on first run
- Seed initial data including:
  - Default roles (Admin, Manager, Staff)
  - Admin user account
  - Sample projects and properties
  - Payment plans and schedules

## Usage

### Dashboard
The main dashboard provides:
- Key performance indicators
- Recent customer registrations
- Payment summaries
- Pending allotments
- Quick action buttons

### Customer Management
- Add new customers with complete profile information
- Track customer status and history
- Manage customer documents and attachments

### Property Management
- Create and manage properties within projects
- Track property status (Available, Allotted, Sold)
- Handle property allotments with approval workflow

### Payment Management
- Create flexible payment plans
- Schedule installments with automatic surcharge calculation
- Record payments with multiple payment methods
- Handle penalties and waivers

### User Management (Admin Only)
- Create and manage user accounts
- Assign roles and permissions
- Track user activity and sessions

## Security Features

- **Password Security**: BCrypt hashing for secure password storage
- **Session Management**: Secure session tracking with automatic timeout
- **Role-Based Access**: Granular permissions based on user roles
- **Activity Logging**: Complete audit trail of all user actions
- **SQL Injection Protection**: Entity Framework parameterized queries

## Development

### Project Structure
```
PMS/
├── Controllers/          # MVC Controllers
├── Models/              # Entity Models
├── Views/               # Razor Views
├── Data/                # DbContext and Database Configuration
├── Services/            # Business Logic Services
├── wwwroot/            # Static Files (CSS, JS, Images)
└── Migrations/         # Entity Framework Migrations
```

### Adding New Features
1. Create/update Entity Models in `Models/` folder
2. Update `PMSDbContext` in `Data/` folder
3. Create/update Controllers in `Controllers/` folder
4. Create/update Views in `Views/` folder
5. Add business logic in `Services/` folder
6. Update database with migrations

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is proprietary software developed for Juba Smart City development.

## Support

For support and questions, please contact the development team.

---

**ZKB - Property Management System** - Streamlining real estate management for the future of Juba.
