using Microsoft.EntityFrameworkCore;
using PMS.Models;
using PMS.Models.Acc;

namespace PMS.Data
{
    public class PMSDbContext : DbContext
    {
        public PMSDbContext(DbContextOptions<PMSDbContext> options) : base(options)
        {
        }

        // Users & Access Control
        public DbSet<ACL> ACLs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserModulePermission> UserModulePermissions { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<UserMacWhitelist> UserMacWhitelists { get; set; }
        public DbSet<BlockedMacLoginAttempt> BlockedMacLoginAttempts { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Registration & Customers
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<PaymentPlan> PaymentPlans { get; set; }
        public DbSet<PaymentSchedule> PaymentSchedules { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerUpdateRequest> CustomerUpdateRequests { get; set; }
        public DbSet<CustomerUpdateRequestChange> CustomerUpdateRequestChanges { get; set; }
        public DbSet<JointOwner> JointOwners { get; set; }
        public DbSet<CustomerLog> CustomerLogs { get; set; }
        public DbSet<BlockingLog> BlockingLogs { get; set; }

        // Projects & Properties
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectSubProject> ProjectSubProjects { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Allotment> Allotments { get; set; }
        public DbSet<Balloting> Ballotings { get; set; }
        public DbSet<Possession> Possessions { get; set; }

        // Payment Management
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Penalty> Penalties { get; set; }
        public DbSet<Waiver> Waivers { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<RefundCheque> RefundCheques { get; set; }
        public DbSet<DuplicateFileTransfer> DuplicateFileTransfers { get; set; }

        // Transfer & NDC
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<NDC> NDCs { get; set; }
        public DbSet<TransferFee> TransferFees { get; set; }

        // Approvals & Workflow
        public DbSet<Approval> Approvals { get; set; }

        // Attachments & Media
        public DbSet<Attachment> Attachments { get; set; }

        // Configuration
        public DbSet<Configuration> Configurations { get; set; }

        // Sales & Leads
        public DbSet<PropertyInquiry> PropertyInquiries { get; set; }

        // Dealers
        public DbSet<Dealer> Dealers { get; set; }

        // Property Logs
        public DbSet<PropertyLog> PropertyLogs { get; set; }

        // Tickets (Customer Care / CRO)
        public DbSet<Ticket> Tickets { get; set; }

        // Rentals (Organization-owned inventory rentals)
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<RentalPayment> RentalPayments { get; set; }
        public DbSet<TransferJointOwner> TransferJointOwners { get; set; }

        // AMS (acc schema)
        public DbSet<AccAccountCategory> AccAccountCategories { get; set; }
        public DbSet<AccAccountHead> AccAccountHeads { get; set; }
        public DbSet<AccFiscalYear> AccFiscalYears { get; set; }
        public DbSet<AccAccountingPeriod> AccAccountingPeriods { get; set; }
        public DbSet<AccOpeningBalance> AccOpeningBalances { get; set; }
        public DbSet<AccBankAccount> AccBankAccounts { get; set; }
        public DbSet<AccChequeBook> AccChequeBooks { get; set; }
        public DbSet<AccChequeRegister> AccChequeRegisters { get; set; }
        public DbSet<AccVoucherType> AccVoucherTypes { get; set; }
        public DbSet<AccVoucher> AccVouchers { get; set; }
        public DbSet<AccVoucherLine> AccVoucherLines { get; set; }
        public DbSet<AccARInvoice> AccARInvoices { get; set; }
        public DbSet<AccARReceipt> AccARReceipts { get; set; }
        public DbSet<AccARReceiptAllocation> AccARReceiptAllocations { get; set; }
        public DbSet<AccVendor> AccVendors { get; set; }
        public DbSet<AccAPBill> AccAPBills { get; set; }
        public DbSet<AccAPBillLine> AccAPBillLines { get; set; }
        public DbSet<AccAPPayment> AccAPPayments { get; set; }
        public DbSet<AccAPPaymentAllocation> AccAPPaymentAllocations { get; set; }
        public DbSet<AccTaxType> AccTaxTypes { get; set; }
        public DbSet<AccTaxTransaction> AccTaxTransactions { get; set; }
        public DbSet<AccDealerCommissionVoucher> AccDealerCommissionVouchers { get; set; }
        public DbSet<AccRefundVoucher> AccRefundVouchers { get; set; }
        public DbSet<AccCostCenter> AccCostCenters { get; set; }
        public DbSet<AccBudget> AccBudgets { get; set; }
        public DbSet<AccBudgetLine> AccBudgetLines { get; set; }
        public DbSet<AccBankReconciliation> AccBankReconciliations { get; set; }
        public DbSet<AccBankReconciliationLine> AccBankReconciliationLines { get; set; }
        public DbSet<AccAccountingAuditLog> AccAccountingAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ACL
            modelBuilder.Entity<ACL>(entity =>
            {
                entity.HasKey(e => e.RoleID);
                entity.Property(e => e.RoleID).HasMaxLength(10);
                entity.Property(e => e.RoleName).HasMaxLength(100);
            });

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.UserID).HasMaxLength(10);
                entity.Property(e => e.FullName).HasMaxLength(150);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.PasswordHash).HasMaxLength(256);
                entity.Property(e => e.RoleID).HasMaxLength(10);
                entity.Property(e => e.Designation).HasMaxLength(150);
                entity.Property(e => e.Department).HasMaxLength(150);
                entity.Property(e => e.UserType).HasMaxLength(50);
                entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
                entity.Property(e => e.TwoFactorSecret).HasMaxLength(500);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasOne(e => e.Role)
                      .WithMany()
                      .HasForeignKey(e => e.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(e => e.ModulePermissions)
                      .WithOne(e => e.User)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserModulePermission (UserID must match Users.UserID type: char(10))
            modelBuilder.Entity<UserModulePermission>(entity =>
            {
                entity.HasKey(e => new { e.UserID, e.ModuleKey });
                entity.Property(e => e.UserID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.ModuleKey).HasMaxLength(50);
                entity.Property(e => e.Permission).HasMaxLength(20);
            });

            // Configure UserSession
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.SessionID);
                entity.Property(e => e.SessionID).HasMaxLength(10);
                entity.Property(e => e.UserID).HasMaxLength(10);
                entity.Property(e => e.IPAddress).HasMaxLength(50);
                entity.Property(e => e.DeviceInfo).HasMaxLength(150);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserSessions)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserMacWhitelist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.MacAddress).HasMaxLength(128).IsRequired();
                entity.Property(e => e.DeviceName).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.AddedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.HasIndex(e => new { e.UserID, e.MacAddress }).IsUnique();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.MacWhitelists)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BlockedMacLoginAttempt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.MacAddress).HasMaxLength(128).IsRequired();
                entity.Property(e => e.DeviceName).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.IPAddress).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.UserAgent).HasMaxLength(500).IsRequired(false);
                entity.Property(e => e.WhitelistedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.HasIndex(e => new { e.UserID, e.AttemptedAt });

                entity.HasOne(e => e.User)
                      .WithMany(u => u.BlockedMacLoginAttempts)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Registration (allow nulls for columns that may be NULL in DB; key RegID must stay required)
            modelBuilder.Entity<Registration>(entity =>
            {
                entity.HasKey(e => e.RegID);
                entity.Property(e => e.RegID).HasMaxLength(10);
                entity.Property(e => e.FullName).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.CNIC).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Phone).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Email).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.FormNo).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.ProjectID).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.Size).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.SubProject).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired(false).HasDefaultValue("Pending");
                entity.HasOne(e => e.Project)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure PaymentPlan (key PlanID must stay required; allow nulls for Currency etc.)
            modelBuilder.Entity<PaymentPlan>(entity =>
            {
                entity.HasKey(e => e.PlanID);
                entity.Property(e => e.PlanID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.RegisteredSize).HasMaxLength(100);
                entity.Property(e => e.SubProject).HasMaxLength(100);
                entity.Property(e => e.PlanName).HasMaxLength(150);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.Frequency).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.HasOne(e => e.Project)
                      .WithMany(p => p.PaymentPlans)
                      .HasForeignKey(e => e.ProjectID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure PaymentSchedule
            modelBuilder.Entity<PaymentSchedule>(entity =>
            {
                entity.HasKey(e => e.ScheduleID);
                entity.Property(e => e.ScheduleID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.PlanID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.PaymentDescription).HasMaxLength(250);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SurchargeRate).HasColumnType("decimal(18,6)");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.HasOne(e => e.PaymentPlan)
                      .WithMany(p => p.PaymentSchedules)
                      .HasForeignKey(e => e.PlanID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Customer (allow nulls for columns that may be NULL in DB to avoid SqlNullValueException; key CustomerID must stay required)
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerID);
                entity.Property(e => e.CustomerID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.RegID).HasMaxLength(10);
                entity.Property(e => e.PlanID).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.ProjectID).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.FullName).HasMaxLength(150);
                entity.Property(e => e.FatherName).HasMaxLength(150);
                entity.Property(e => e.CNIC).HasMaxLength(50);
                entity.Property(e => e.PassportNo).HasMaxLength(50);
                entity.Property(e => e.Gender).HasMaxLength(20);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.FormNo).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.MailingAddress).HasMaxLength(255);
                entity.Property(e => e.PermanentAddress).HasMaxLength(255);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.SubProject).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.RegisteredSize).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.NomineeName).HasMaxLength(100);
                entity.Property(e => e.NomineeID).HasMaxLength(50);
                entity.Property(e => e.NomineeRelation).HasMaxLength(50);
                entity.Property(e => e.DealerID).IsRequired(false);
                entity.Property(e => e.IsDealerRegistered).IsRequired(false);
                entity.Property(e => e.DealerName).HasMaxLength(200).IsRequired(false);

                entity.HasOne(e => e.Registration)
                      .WithMany(r => r.Customers)
                      .HasForeignKey(e => e.RegID)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.PaymentPlan)
                      .WithMany(p => p.Customers)
                      .HasForeignKey(e => e.PlanID)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Dealer)
                      .WithMany(d => d.Customers)
                      .HasForeignKey(e => e.DealerID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure CustomerLog
            modelBuilder.Entity<CustomerLog>(entity =>
            {
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.Action).HasMaxLength(150);
                entity.Property(e => e.Remarks).HasMaxLength(255);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.CustomerLogs)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CustomerUpdateRequest>(entity =>
            {
                entity.HasKey(e => e.RequestID);
                entity.Property(e => e.RequestID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.CustomerID).HasMaxLength(10).HasColumnType("char(10)").IsRequired();
                entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("Pending");
                entity.Property(e => e.RequestedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.RejectedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.CustomerUpdateRequests)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RequestedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.RequestedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.RejectedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.RejectedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.CustomerID, e.Status, e.RequestedAt })
                      .HasDatabaseName("IX_CustomerUpdateRequests_Customer_Status");
            });

            modelBuilder.Entity<CustomerUpdateRequestChange>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.RequestID).HasMaxLength(10).HasColumnType("char(10)").IsRequired();
                entity.Property(e => e.FieldName).HasMaxLength(100).IsRequired();

                entity.HasOne(e => e.Request)
                      .WithMany(r => r.Changes)
                      .HasForeignKey(e => e.RequestID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.RequestID)
                      .HasDatabaseName("IX_CustomerUpdateRequestChanges_RequestID");
            });

            modelBuilder.Entity<JointOwner>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(10);
                entity.Property(e => e.CustomerID).HasMaxLength(10).IsRequired();
                entity.Property(e => e.JointOwnerName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.FatherName).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.CNIC).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Contact).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Address).HasMaxLength(255).IsRequired(false);
                entity.Property(e => e.Percentage).HasColumnType("decimal(5,2)").IsRequired(false);
                entity.Property(e => e.CreatedBy).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.Details).HasColumnType("nvarchar(max)").IsRequired(false);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.JointOwners)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TransferJointOwner>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(10);
                entity.Property(e => e.TransferID).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CustomerID).HasMaxLength(10).IsRequired();
                entity.Property(e => e.JointOwnerName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.FatherName).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.CNIC).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Contact).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Address).HasMaxLength(255).IsRequired(false);
                entity.Property(e => e.Percentage).HasColumnType("decimal(5,2)").IsRequired(false);
                entity.Property(e => e.CreatedBy).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.Details).HasColumnType("nvarchar(max)").IsRequired(false);

                entity.HasOne(e => e.Transfer)
                      .WithMany(t => t.TransferJointOwners)
                      .HasForeignKey(e => e.TransferID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.TransferJointOwners)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BlockingLog>(entity =>
            {
                entity.HasKey(e => e.BlockingLogID);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.UserID).HasMaxLength(10);
                entity.Property(e => e.PreviousStatus).HasMaxLength(50);
                entity.Property(e => e.NewStatus).HasMaxLength(50);
                entity.Property(e => e.AttachmentPath).HasMaxLength(500);
                entity.HasOne(e => e.Customer)
                      .WithMany()
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Project (key ProjectID must stay required; other string columns may be null in DB)
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.ProjectID);
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.ProjectName).HasMaxLength(150);
                entity.Property(e => e.Prefix).HasMaxLength(7);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.Location).HasMaxLength(150);
                entity.Property(e => e.Sizes).HasMaxLength(1000);
                entity.Property(e => e.SubProjects).HasMaxLength(1000);
                entity.Property(e => e.PropertyTypes).HasMaxLength(500);
            });

            modelBuilder.Entity<ProjectSubProject>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(10);
                entity.Property(e => e.ProjectID).HasMaxLength(10).IsRequired();
                entity.Property(e => e.SubProjectName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Prefix).HasMaxLength(7).IsRequired();

                entity.HasIndex(e => new { e.ProjectID, e.SubProjectName }).IsUnique();
                entity.HasIndex(e => new { e.ProjectID, e.Prefix }).IsUnique();

                entity.HasOne(e => e.Project)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Property
            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasKey(e => e.PropertyID);
                entity.Property(e => e.PropertyID).HasMaxLength(10);
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.SubProject).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.PlotNo).HasMaxLength(50);
                entity.Property(e => e.Street).HasMaxLength(50);
                entity.Property(e => e.PlotType).HasMaxLength(50);
                entity.Property(e => e.Block).HasMaxLength(50);
                entity.Property(e => e.PropertyType).HasMaxLength(50);
                entity.Property(e => e.Size).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Properties)
                      .HasForeignKey(e => e.ProjectID)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Dealer)
                      .WithMany(d => d.Properties)
                      .HasForeignKey(e => e.DealerID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Allotment (key AllotmentID must stay required; allow nulls for other string columns that may be NULL in DB)
            modelBuilder.Entity<Allotment>(entity =>
            {
                entity.HasKey(e => e.AllotmentID);
                entity.Property(e => e.AllotmentID).HasMaxLength(10);
                entity.Property(e => e.PropertyID).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.CustomerID).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.AllottedBy).HasMaxLength(10).IsRequired(false);
                entity.Property(e => e.ApprovedBy).HasMaxLength(50);
                entity.Property(e => e.AllottmentType).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.WorkFlowStatus).HasMaxLength(250).IsRequired(false);

                entity.HasOne(e => e.Property)
                      .WithMany(p => p.Allotments)
                      .HasForeignKey(e => e.PropertyID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Allotments)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AllottedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.AllottedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Balloting
            modelBuilder.Entity<Balloting>(entity =>
            {
                entity.HasKey(e => e.BallotID);
                entity.Property(e => e.BallotID).HasMaxLength(10);
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.ConductedBy).HasMaxLength(10);
                entity.Property(e => e.Remarks).HasMaxLength(255);

                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Ballotings)
                      .HasForeignKey(e => e.ProjectID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ConductedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ConductedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Possession
            modelBuilder.Entity<Possession>(entity =>
            {
                entity.HasKey(e => e.PossessionID);
                entity.Property(e => e.PossessionID).HasMaxLength(40);
                entity.Property(e => e.PropertyID).HasMaxLength(10);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.WorkFlowStatus).HasMaxLength(100);
                entity.Property(e => e.Remarks).HasMaxLength(255);
                entity.Property(e => e.PossessionDueCharges).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PossessionPaidCharges).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BankName).HasMaxLength(200);
                entity.Property(e => e.InstrumentNo).HasMaxLength(100);
                entity.Property(e => e.PaymentMethod).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.ModifiedBy).HasMaxLength(10);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10);
                entity.Property(e => e.DeclinedBy).HasMaxLength(10);

                entity.HasOne(e => e.Property)
                      .WithMany(p => p.Possessions)
                      .HasForeignKey(e => e.PropertyID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Possessions)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ModifiedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ModifiedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.DeclinedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.DeclinedBy)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable(tb => tb.HasTrigger("TR_Payments_Metadata"));
                entity.HasKey(e => e.PaymentID);
                entity.Property(e => e.PaymentID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.ScheduleID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.CustomerID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Method).HasMaxLength(50).HasColumnName("PaymentMethod");
                entity.Property(e => e.ReferenceNo).HasMaxLength(100).HasColumnName("ReferenceNumber");
                entity.Property(e => e.Status).HasMaxLength(250);
                entity.Property(e => e.Remarks).HasMaxLength(255);

                entity.Property(e => e.AuditStatus).HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(e => e.AuditedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.AuditRemarks).HasMaxLength(500).IsRequired(false);

                entity.HasOne(e => e.PaymentSchedule)
                      .WithMany(p => p.Payments)
                      .HasForeignKey(e => e.ScheduleID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                      .WithMany()
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AuditedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.AuditedBy)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure Penalty
            modelBuilder.Entity<Penalty>(entity =>
            {
                entity.HasKey(e => e.PenaltyID);
                entity.Property(e => e.PenaltyID).HasMaxLength(10);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Reason).HasMaxLength(255);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Penalties)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Waiver
            modelBuilder.Entity<Waiver>(entity =>
            {
                entity.HasKey(e => e.WaiverID);
                entity.Property(e => e.WaiverID).HasMaxLength(10);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.WaiverType).HasMaxLength(255);
                entity.Property(e => e.Status).HasMaxLength(100).HasDefaultValue("Initiated");
                entity.Property(e => e.AccountHead).HasMaxLength(255).HasDefaultValue("Waived Off");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WaivedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WaivedPercentage).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Comments).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ApprovedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.LastModifiedBy).HasMaxLength(10);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Waivers)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.LastModifiedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.LastModifiedBy)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure Refund
            modelBuilder.Entity<Refund>(entity =>
            {
                entity.HasKey(e => e.RefundID);
                entity.Property(e => e.RefundID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.CustomerID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.RefundType).HasMaxLength(50);
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DeductionAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.RefundedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Reason).HasMaxLength(255);
                entity.Property(e => e.WorkflowStatus).HasMaxLength(100).HasDefaultValue("Initiated");
                entity.Property(e => e.CreatedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.Notes).HasMaxLength(500).IsRequired(false);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Refunds)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(e => e.RefundCheques)
                      .WithOne(c => c.Refund)
                      .HasForeignKey(c => c.RefundID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefundCheque>(entity =>
            {
                entity.ToTable("RefundCheques");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RefundID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.ChequeNo).HasMaxLength(100);
                entity.Property(e => e.ChequeDate).HasColumnType("date");
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Bank).HasMaxLength(200).IsRequired(false);
                entity.Property(e => e.Details).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
            });

            modelBuilder.Entity<DuplicateFileTransfer>(entity =>
            {
                entity.ToTable("DuplicateFileTransfer");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.CustomerID).HasMaxLength(10).HasColumnType("char(10)").IsRequired();
                entity.Property(e => e.CustomerName).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.CustomerCNIC).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.CreatedAt).HasColumnName("Created_at").IsRequired();
                entity.Property(e => e.CreatedBy).HasColumnName("Created_by").HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasColumnName("Modified_by").HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Comments).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(e => e.FeeDue).HasColumnType("decimal(18,2)").IsRequired(false);
                entity.Property(e => e.FeePaid).HasColumnType("decimal(18,2)").IsRequired(false);
                entity.Property(e => e.ChallanID).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.BankName).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.InstrumentNo).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.DepositDate).HasColumnType("date").IsRequired(false);
                entity.Property(e => e.PaymentMethod).HasMaxLength(100).IsRequired(false);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.DuplicateFileTransfers)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Transfer (customer file / ownership transfer)
            modelBuilder.Entity<Transfer>(entity =>
            {
                entity.HasKey(e => e.TransferID);
                entity.Property(e => e.TransferID).HasMaxLength(50);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.WorkFlowStatus).HasMaxLength(100);
                entity.Property(e => e.BuyerGender).HasMaxLength(20);
                entity.Property(e => e.BuyerNationality).HasMaxLength(100);
                entity.Property(e => e.BuyerEmail).HasMaxLength(150);
                entity.Property(e => e.BuyerPhone).HasMaxLength(50);
                entity.Property(e => e.BuyerMobile).HasMaxLength(50);
                entity.Property(e => e.BuyerMobile2).HasMaxLength(50);
                entity.Property(e => e.BuyerMailingAddress).HasMaxLength(255);
                entity.Property(e => e.BuyerPermanentAddress).HasMaxLength(255);
                entity.Property(e => e.BuyerDOB).HasColumnType("date");
                entity.Property(e => e.SellerBiometric).HasColumnType("nvarchar(max)");
                entity.Property(e => e.BuyerBiometric).HasColumnType("nvarchar(max)");
                entity.Property(e => e.PaymentMethod).HasMaxLength(200).IsRequired(false);
                entity.Property(e => e.BankName).HasMaxLength(200).IsRequired(false);
                entity.Property(e => e.PaymentDetails).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(e => e.PaymentDate).HasColumnType("date").IsRequired(false);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Transfers)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure NDC
            modelBuilder.Entity<NDC>(entity =>
            {
                entity.HasKey(e => e.NDCID);
                entity.Property(e => e.NDCID).HasMaxLength(10);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.NDCType).HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.WorkFlowStatus).HasMaxLength(500);
                entity.Property(e => e.Remarks).HasMaxLength(255);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.TotalDueAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalDueInstallments).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RemainingDues).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AmountPerUnit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TransferFeeAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PropertySize).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.NDCs)
                      .HasForeignKey(e => e.CustomerID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TransferFee
            modelBuilder.Entity<TransferFee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.ProjectID).HasMaxLength(10).HasColumnType("char(10)");
                entity.Property(e => e.SubProject).HasMaxLength(100);
                entity.Property(e => e.TransferType).HasMaxLength(100);
                entity.Property(e => e.TransferPriority).HasMaxLength(20);
                entity.Property(e => e.AmountPerUnit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasMaxLength(10).HasColumnType("char(10)").IsRequired(false);
                entity.HasOne(e => e.Project)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Approval
            modelBuilder.Entity<Approval>(entity =>
            {
                entity.HasKey(e => e.ApprovalID);
                entity.Property(e => e.ApprovalID).HasMaxLength(10);
                entity.Property(e => e.RefType).HasMaxLength(50);
                entity.Property(e => e.RefID).HasMaxLength(10);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Remarks).HasMaxLength(255);
            });

            // Configure Attachment
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasKey(e => e.AttachmentID);
                entity.Property(e => e.AttachmentID).HasMaxLength(10);
                entity.Property(e => e.RefType).HasMaxLength(50);
                entity.Property(e => e.RefID).HasMaxLength(100);
                entity.Property(e => e.AttachmentType).HasMaxLength(50);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.FilePath).HasMaxLength(255);
                entity.Property(e => e.FileType).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.UploadedBy).HasMaxLength(10);
            });

            // Configure ActivityLog
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.UserID).HasMaxLength(10);
                entity.Property(e => e.Action).HasMaxLength(255);
                entity.Property(e => e.RefType).HasMaxLength(50);
                entity.Property(e => e.RefID).HasMaxLength(100);
                entity.Property(e => e.Details).HasColumnType("nvarchar(max)");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.ActivityLogs)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationID);
                entity.Property(e => e.NotificationID).HasMaxLength(10);
                entity.Property(e => e.UserID).HasMaxLength(10);
                entity.Property(e => e.Message).HasMaxLength(255);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Configuration
            modelBuilder.Entity<Configuration>(entity =>
            {
                entity.HasKey(e => e.ConfigKey);
                entity.Property(e => e.ConfigKey).HasMaxLength(50);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(10);

                entity.HasOne(e => e.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.UpdatedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure PropertyInquiry
            modelBuilder.Entity<PropertyInquiry>(entity =>
            {
                entity.HasKey(e => e.InquiryID);
                entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
                entity.Property(e => e.EmailAddress).HasMaxLength(150);
                entity.Property(e => e.InquiryType).HasMaxLength(100);
                entity.Property(e => e.IPAddress).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("New");
                entity.Property(e => e.AssignedTo).HasMaxLength(100);
                entity.Property(e => e.IsContacted).HasDefaultValue(false);
            });

            // Configure Dealer
            modelBuilder.Entity<Dealer>(entity =>
            {
                entity.HasKey(e => e.DealerID);
                entity.Property(e => e.DealershipName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.RegisterationDate).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
                entity.Property(e => e.MembershipType).HasMaxLength(10).IsRequired();
                entity.Property(e => e.OwnerName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.OwnerCNIC).HasMaxLength(50).IsRequired();
                entity.Property(e => e.MobileNo).HasMaxLength(50).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(50).IsRequired();
                entity.Property(e => e.OwnerDetails).IsRequired();
            });

            // Configure PropertyLog
            modelBuilder.Entity<PropertyLog>(entity =>
            {
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.PropertyID).HasMaxLength(10);
                entity.Property(e => e.Action).HasMaxLength(150);
                entity.Property(e => e.OldValue).HasMaxLength(255);
                entity.Property(e => e.NewValue).HasMaxLength(255);
                entity.Property(e => e.Remarks).HasMaxLength(255);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.HasOne(e => e.Property)
                      .WithMany(p => p.PropertyLogs)
                      .HasForeignKey(e => e.PropertyID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Ticket
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.TicketID);
                entity.Property(e => e.TicketID).HasMaxLength(10);
                entity.Property(e => e.CustomerID).HasMaxLength(150);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.Contact).HasMaxLength(256);
                entity.Property(e => e.Status).HasMaxLength(256);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
                entity.Property(e => e.AssignedTo).HasMaxLength(256);
            });

            // Configure Rental
            modelBuilder.Entity<Rental>(entity =>
            {
                entity.HasKey(e => e.RentalID);
                entity.Property(e => e.RentalID).HasMaxLength(50);
                // IMPORTANT: match existing SQL type in Property table (CHAR(10))
                entity.Property(e => e.PropertyID).HasMaxLength(10).HasColumnType("char(10)").IsRequired();
                entity.Property(e => e.TenantName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.TenantCNIC).HasMaxLength(50);
                entity.Property(e => e.TenantPhone).HasMaxLength(50);
                entity.Property(e => e.TenantEmail).HasMaxLength(150);
                entity.Property(e => e.TenantAddress).HasMaxLength(255);
                entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SecurityDeposit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AdvanceRent).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("PKR");
                entity.Property(e => e.StartDate).HasColumnType("date");
                entity.Property(e => e.EndDate).HasColumnType("date");
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue(Rental.StatusActive);

                entity.HasIndex(e => e.PropertyID);
                entity.HasIndex(e => new { e.Status, e.StartDate });

                // Industry rule: only one active rental per property at a time
                entity.HasIndex(e => e.PropertyID)
                      .IsUnique()
                      .HasFilter("[Status] = 'Active'");

                entity.HasOne(e => e.Property)
                      .WithMany(p => p.Rentals)
                      .HasForeignKey(e => e.PropertyID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure RentalPayment
            modelBuilder.Entity<RentalPayment>(entity =>
            {
                entity.HasKey(e => e.RentalPaymentID);
                entity.Property(e => e.RentalPaymentID).HasMaxLength(50);
                entity.Property(e => e.RentalID).HasMaxLength(50).IsRequired();
                entity.Property(e => e.DueDate).HasColumnType("date");
                entity.Property(e => e.AmountDue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.ReferenceNo).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue(RentalPayment.StatusPending);
                entity.Property(e => e.Remarks).HasMaxLength(255);

                entity.HasIndex(e => e.RentalID);
                entity.HasIndex(e => new { e.BillingYear, e.BillingMonth });
                entity.HasIndex(e => new { e.Status, e.DueDate });

                entity.HasOne(e => e.Rental)
                      .WithMany(r => r.RentalPayments)
                      .HasForeignKey(e => e.RentalID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AMS — acc schema
            modelBuilder.Entity<AccAccountCategory>(entity =>
            {
                entity.ToTable("AccountCategory", "acc");
                entity.HasKey(e => e.AccountCategoryID);
                entity.Property(e => e.CategoryName).HasMaxLength(100);
                entity.Property(e => e.NatureType).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<AccAccountHead>(entity =>
            {
                entity.ToTable("AccountHead", "acc");
                entity.HasKey(e => e.AccountHeadID);
                entity.Property(e => e.AccountCode).HasMaxLength(30);
                entity.Property(e => e.AccountName).HasMaxLength(150);
                entity.Property(e => e.OpeningBalanceType).HasMaxLength(10);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OpeningBalanceDate).HasColumnType("date");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.AccountCode).IsUnique();
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.AccountHeads)
                    .HasForeignKey(e => e.AccountCategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(e => e.ParentAccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccFiscalYear>(entity =>
            {
                entity.ToTable("FiscalYear", "acc");
                entity.HasKey(e => e.FiscalYearID);
                entity.Property(e => e.YearName).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Open");
                entity.Property(e => e.ClosedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.StartDate).HasColumnType("date");
                entity.Property(e => e.EndDate).HasColumnType("date");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<AccAccountingPeriod>(entity =>
            {
                entity.ToTable("AccountingPeriod", "acc");
                entity.HasKey(e => e.PeriodID);
                entity.Property(e => e.PeriodName).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Open");
                entity.Property(e => e.ClosedBy).HasMaxLength(10);
                entity.Property(e => e.StartDate).HasColumnType("date");
                entity.Property(e => e.EndDate).HasColumnType("date");
                entity.HasOne(e => e.FiscalYear)
                    .WithMany(f => f.Periods)
                    .HasForeignKey(e => e.FiscalYearID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AccOpeningBalance>(entity =>
            {
                entity.ToTable("OpeningBalance", "acc");
                entity.HasKey(e => e.OpeningBalanceID);
                entity.Property(e => e.SubLedgerType).HasMaxLength(30);
                entity.Property(e => e.SubLedgerID).HasMaxLength(10);
                entity.Property(e => e.DebitAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreditAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(300);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.FiscalYear)
                    .WithMany()
                    .HasForeignKey(e => e.FiscalYearID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccBankAccount>(entity =>
            {
                entity.ToTable("BankAccount", "acc");
                entity.HasKey(e => e.BankAccountID);
                entity.Property(e => e.BankName).HasMaxLength(150);
                entity.Property(e => e.BranchName).HasMaxLength(150);
                entity.Property(e => e.BranchCode).HasMaxLength(20);
                entity.Property(e => e.AccountTitle).HasMaxLength(200);
                entity.Property(e => e.AccountNumber).HasMaxLength(50);
                entity.Property(e => e.IBAN).HasMaxLength(34);
                entity.Property(e => e.AccountType).HasMaxLength(30);
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("PKR");
                entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OpeningDate).HasColumnType("date");
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccChequeBook>(entity =>
            {
                entity.ToTable("ChequeBook", "acc");
                entity.HasKey(e => e.ChequeBookID);
                entity.Property(e => e.SeriesFrom).HasMaxLength(20);
                entity.Property(e => e.SeriesTo).HasMaxLength(20);
                entity.Property(e => e.IssuedDate).HasColumnType("date");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.BankAccount)
                    .WithMany(b => b.ChequeBooks)
                    .HasForeignKey(e => e.BankAccountID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccChequeRegister>(entity =>
            {
                entity.ToTable("ChequeRegister", "acc");
                entity.HasKey(e => e.ChequeRegisterID);
                entity.Property(e => e.ChequeNo).HasMaxLength(30);
                entity.Property(e => e.ChequeDate).HasColumnType("date");
                entity.Property(e => e.EntryDate).HasColumnType("date");
                entity.Property(e => e.ClearanceDate).HasColumnType("date");
                entity.Property(e => e.ChequeType).HasMaxLength(20);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PayableTo).HasMaxLength(200);
                entity.Property(e => e.ReceivedFrom).HasMaxLength(200);
                entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("Pending");
                entity.Property(e => e.BounceReason).HasMaxLength(300);
                entity.Property(e => e.BounceChargeAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SubLedgerType).HasMaxLength(30);
                entity.Property(e => e.SubLedgerID).HasMaxLength(10);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.BankAccount)
                    .WithMany(b => b.ChequeRegisters)
                    .HasForeignKey(e => e.BankAccountID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ChequeBook)
                    .WithMany(b => b.ChequeRegisters)
                    .HasForeignKey(e => e.ChequeBookID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ReplacedByCheque)
                    .WithMany()
                    .HasForeignKey(e => e.ReplacedByChequeID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Voucher)
                    .WithMany()
                    .HasForeignKey(e => e.VoucherID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AccVoucherType>(entity =>
            {
                entity.ToTable("VoucherType", "acc");
                entity.HasKey(e => e.VoucherTypeID);
                entity.Property(e => e.TypeCode).HasMaxLength(10);
                entity.Property(e => e.TypeName).HasMaxLength(100);
                entity.Property(e => e.Prefix).HasMaxLength(10);
                entity.HasIndex(e => e.TypeCode).IsUnique();
            });

            modelBuilder.Entity<AccVoucher>(entity =>
            {
                entity.ToTable("Voucher", "acc");
                entity.HasKey(e => e.VoucherID);
                entity.Property(e => e.VoucherNo).HasMaxLength(30);
                entity.Property(e => e.VoucherDate).HasColumnType("date");
                entity.Property(e => e.ReferenceNo).HasMaxLength(100);
                entity.Property(e => e.Narration).HasMaxLength(1000);
                entity.Property(e => e.TotalDebit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalCredit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Draft");
                entity.Property(e => e.SourceModule).HasMaxLength(50);
                entity.Property(e => e.PMSProjectID).HasMaxLength(10);
                entity.Property(e => e.PMSAllotmentID).HasMaxLength(10);
                entity.Property(e => e.PMSCustomerID).HasMaxLength(10);
                entity.Property(e => e.PMSPaymentID).HasMaxLength(10);
                entity.Property(e => e.PMSRefundID).HasMaxLength(10);
                entity.Property(e => e.PMSDealerPaymentID).HasMaxLength(10);
                entity.Property(e => e.PMSPenaltyID).HasMaxLength(10);
                entity.Property(e => e.PMSTransferID).HasMaxLength(50);
                entity.Property(e => e.PMSRentalPaymentID).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.SubmittedBy).HasMaxLength(10);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10);
                entity.Property(e => e.PostedBy).HasMaxLength(10);
                entity.Property(e => e.ReversedBy).HasMaxLength(10);
                entity.HasIndex(e => e.VoucherNo).IsUnique();
                entity.HasOne(e => e.VoucherType)
                    .WithMany(t => t.Vouchers)
                    .HasForeignKey(e => e.VoucherTypeID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Period)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.FiscalYear)
                    .WithMany()
                    .HasForeignKey(e => e.FiscalYearID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ReversalOf)
                    .WithMany()
                    .HasForeignKey(e => e.ReversalVoucherID)
                    .OnDelete(DeleteBehavior.Restrict);
                if (PMSDbContextAccCompat.MapVoucherBankAccountColumn)
                {
                    entity.HasOne(e => e.BankAccount)
                        .WithMany()
                        .HasForeignKey(e => e.BankAccountID)
                        .OnDelete(DeleteBehavior.SetNull);
                }
                else
                {
                    entity.Ignore(e => e.BankAccountID);
                    entity.Ignore(e => e.BankAccount);
                }
            });

            modelBuilder.Entity<AccVoucherLine>(entity =>
            {
                entity.ToTable("VoucherLine", "acc");
                entity.HasKey(e => e.VoucherLineID);
                entity.Property(e => e.LineNumber);
                entity.Property(e => e.SubLedgerType).HasMaxLength(30);
                entity.Property(e => e.SubLedgerID).HasMaxLength(10);
                entity.Property(e => e.DebitAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreditAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ExchangeRate).HasColumnType("decimal(18,6)");
                entity.Property(e => e.Currency).HasMaxLength(10);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasOne(e => e.Voucher)
                    .WithMany(v => v.Lines)
                    .HasForeignKey(e => e.VoucherID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccARInvoice>(entity =>
            {
                entity.ToTable("ARInvoice", "acc");
                entity.HasKey(e => e.ARInvoiceID);
                entity.Property(e => e.InvoiceNo).HasMaxLength(30);
                entity.Property(e => e.InvoiceDate).HasColumnType("date");
                entity.Property(e => e.DueDate).HasColumnType("date");
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.AllotmentID).HasMaxLength(10);
                entity.Property(e => e.InvoiceType).HasMaxLength(50);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.PMSPaymentScheduleID).HasMaxLength(10);
                entity.Property(e => e.PMSPenaltyID).HasMaxLength(10);
                entity.Property(e => e.CancellationReason).HasMaxLength(300);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.LastModifiedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.InvoiceNo).IsUnique();
                entity.Ignore(e => e.BalanceAmount);
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccARReceipt>(entity =>
            {
                entity.ToTable("ARReceipt", "acc");
                entity.HasKey(e => e.ARReceiptID);
                entity.Property(e => e.ReceiptNo).HasMaxLength(30);
                entity.Property(e => e.ReceiptDate).HasColumnType("date");
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.AllotmentID).HasMaxLength(10);
                entity.Property(e => e.ReceivedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMode).HasMaxLength(30);
                entity.Property(e => e.ChequeNo).HasMaxLength(30);
                entity.Property(e => e.ChequeDate).HasColumnType("date");
                entity.Property(e => e.BankName).HasMaxLength(150);
                entity.Property(e => e.PMSPaymentID).HasMaxLength(10);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.ReceiptNo).IsUnique();
                entity.HasOne(e => e.BankAccount)
                    .WithMany()
                    .HasForeignKey(e => e.BankAccountID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.ChequeRegister)
                    .WithMany()
                    .HasForeignKey(e => e.ChequeRegisterID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AccARReceiptAllocation>(entity =>
            {
                entity.ToTable("ARReceiptAllocation", "acc");
                entity.HasKey(e => e.AllocationID);
                entity.Property(e => e.AllocatedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AllocatedBy).HasMaxLength(10);
                entity.Property(e => e.AllocatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Receipt)
                    .WithMany(r => r.Allocations)
                    .HasForeignKey(e => e.ARReceiptID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Invoice)
                    .WithMany(i => i.ReceiptAllocations)
                    .HasForeignKey(e => e.ARInvoiceID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccVendor>(entity =>
            {
                entity.ToTable("Vendor", "acc");
                entity.HasKey(e => e.VendorID);
                entity.Property(e => e.VendorCode).HasMaxLength(20);
                entity.Property(e => e.VendorName).HasMaxLength(200);
                entity.Property(e => e.VendorType).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.VendorCode).IsUnique();
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AccAPBill>(entity =>
            {
                entity.ToTable("APBill", "acc");
                entity.HasKey(e => e.APBillID);
                entity.Property(e => e.BillNo).HasMaxLength(30);
                entity.Property(e => e.BillDate).HasColumnType("date");
                entity.Property(e => e.DueDate).HasColumnType("date");
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.BillType).HasMaxLength(50);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WHTAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GSTAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OtherTaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RetentionAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RetentionReleased).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.AttachmentPath).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.BillNo).IsUnique();
                entity.Ignore(e => e.BalanceAmount);
                entity.HasOne(e => e.Vendor)
                    .WithMany(v => v.Bills)
                    .HasForeignKey(e => e.VendorID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Voucher)
                    .WithMany()
                    .HasForeignKey(e => e.VoucherID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AccAPBillLine>(entity =>
            {
                entity.ToTable("APBillLine", "acc");
                entity.HasKey(e => e.APBillLineID);
                entity.Property(e => e.Description).HasMaxLength(300);
                entity.Property(e => e.Quantity).HasColumnType("decimal(10,3)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WHTRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.WHTAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GSTRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.GSTAmount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Bill)
                    .WithMany(b => b.Lines)
                    .HasForeignKey(e => e.APBillID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccAPPayment>(entity =>
            {
                entity.ToTable("APPayment", "acc");
                entity.HasKey(e => e.APPaymentID);
                entity.Property(e => e.PaymentNo).HasMaxLength(30);
                entity.Property(e => e.PaymentDate).HasColumnType("date");
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMode).HasMaxLength(30);
                entity.Property(e => e.ChequeNo).HasMaxLength(30);
                entity.Property(e => e.ChequeDate).HasColumnType("date");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.PaymentNo).IsUnique();
                entity.HasOne(e => e.Vendor)
                    .WithMany()
                    .HasForeignKey(e => e.VendorID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.BankAccount)
                    .WithMany()
                    .HasForeignKey(e => e.BankAccountID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.ChequeRegister)
                    .WithMany()
                    .HasForeignKey(e => e.ChequeRegisterID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Voucher)
                    .WithMany()
                    .HasForeignKey(e => e.VoucherID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AccAPPaymentAllocation>(entity =>
            {
                entity.ToTable("APPaymentAllocation", "acc");
                entity.HasKey(e => e.AllocationID);
                entity.Property(e => e.AllocatedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AllocatedBy).HasMaxLength(10);
                entity.Property(e => e.AllocatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Payment)
                    .WithMany(p => p.Allocations)
                    .HasForeignKey(e => e.APPaymentID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Bill)
                    .WithMany()
                    .HasForeignKey(e => e.APBillID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccTaxType>(entity =>
            {
                entity.ToTable("TaxType", "acc");
                entity.HasKey(e => e.TaxTypeID);
                entity.Property(e => e.TaxCode).HasMaxLength(20);
                entity.Property(e => e.TaxName).HasMaxLength(100);
                entity.Property(e => e.TaxCategory).HasMaxLength(30);
                entity.Property(e => e.AppliesTo).HasMaxLength(30);
                entity.Property(e => e.Rate).HasColumnType("decimal(7,4)");
                entity.Property(e => e.EffectiveFrom).HasColumnType("date");
                entity.Property(e => e.EffectiveTo).HasColumnType("date");
                entity.HasIndex(e => e.TaxCode).IsUnique();
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccTaxTransaction>(entity =>
            {
                entity.ToTable("TaxTransaction", "acc");
                entity.HasKey(e => e.TaxTransactionID);
                entity.Property(e => e.TaxableAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(7,4)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SubLedgerType).HasMaxLength(30);
                entity.Property(e => e.SubLedgerID).HasMaxLength(10);
                entity.Property(e => e.ChallanNo).HasMaxLength(50);
                entity.Property(e => e.DepositedDate).HasColumnType("date");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Voucher)
                    .WithMany()
                    .HasForeignKey(e => e.VoucherID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.TaxType)
                    .WithMany()
                    .HasForeignKey(e => e.TaxTypeID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Period)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccDealerCommissionVoucher>(entity =>
            {
                entity.ToTable("DealerCommissionVoucher", "acc");
                entity.HasKey(e => e.CommissionVoucherID);
                entity.Property(e => e.VoucherNo).HasMaxLength(30);
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.AllotmentID).HasMaxLength(10);
                entity.Property(e => e.PMSDealerPaymentID).HasMaxLength(10);
                entity.Property(e => e.GrossCommission).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WHTRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.WHTAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NetPayable).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.VoucherNo).IsUnique();
                entity.HasOne(e => e.AccountingVoucher)
                    .WithMany()
                    .HasForeignKey(e => e.AccountingVoucherID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.APPayment)
                    .WithMany()
                    .HasForeignKey(e => e.APPaymentID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccRefundVoucher>(entity =>
            {
                entity.ToTable("RefundVoucher", "acc");
                entity.HasKey(e => e.RefundVoucherID);
                entity.Property(e => e.VoucherNo).HasMaxLength(30);
                entity.Property(e => e.CustomerID).HasMaxLength(10);
                entity.Property(e => e.AllotmentID).HasMaxLength(10);
                entity.Property(e => e.PMSRefundID).HasMaxLength(10);
                entity.Property(e => e.GrossRefundAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ProcessingFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PenaltyDeduction).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OtherDeduction).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NetRefundAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMode).HasMaxLength(30);
                entity.Property(e => e.ChequeNo).HasMaxLength(30);
                entity.Property(e => e.ChequeDate).HasColumnType("date");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.VoucherNo).IsUnique();
                entity.HasOne(e => e.BankAccount)
                    .WithMany()
                    .HasForeignKey(e => e.BankAccountID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ChequeRegister)
                    .WithMany()
                    .HasForeignKey(e => e.ChequeRegisterID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.AccountingVoucher)
                    .WithMany()
                    .HasForeignKey(e => e.AccountingVoucherID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccCostCenter>(entity =>
            {
                entity.ToTable("CostCenter", "acc");
                entity.HasKey(e => e.CostCenterID);
                entity.Property(e => e.CostCenterCode).HasMaxLength(20);
                entity.Property(e => e.CostCenterName).HasMaxLength(150);
                entity.Property(e => e.ProjectID).HasMaxLength(10);
                entity.Property(e => e.SubProjectID).HasMaxLength(10);
                entity.Property(e => e.CostCenterType).HasMaxLength(30);
                entity.Property(e => e.BudgetAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasIndex(e => e.CostCenterCode).IsUnique();
                entity.HasOne<AccCostCenter>()
                    .WithMany()
                    .HasForeignKey(e => e.ParentCostCenterID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccBudget>(entity =>
            {
                entity.ToTable("Budget", "acc");
                entity.HasKey(e => e.BudgetID);
                entity.Property(e => e.BudgetName).HasMaxLength(150);
                entity.Property(e => e.BudgetType).HasMaxLength(30);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.ApprovedBy).HasMaxLength(10);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.FiscalYear)
                    .WithMany()
                    .HasForeignKey(e => e.FiscalYearID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccBudgetLine>(entity =>
            {
                entity.ToTable("BudgetLine", "acc");
                entity.HasKey(e => e.BudgetLineID);
                entity.Property(e => e.BudgetedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RevisedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Remarks).HasMaxLength(300);
                entity.HasOne(e => e.Budget)
                    .WithMany(b => b.Lines)
                    .HasForeignKey(e => e.BudgetID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AccountHead)
                    .WithMany()
                    .HasForeignKey(e => e.AccountHeadID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CostCenter)
                    .WithMany()
                    .HasForeignKey(e => e.CostCenterID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Period)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AccBankReconciliation>(entity =>
            {
                entity.ToTable("BankReconciliation", "acc");
                entity.HasKey(e => e.ReconciliationID);
                entity.Property(e => e.StatementDate).HasColumnType("date");
                entity.Property(e => e.BankStatementBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BookBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.ReconciledBy).HasMaxLength(10);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.BankAccount)
                    .WithMany()
                    .HasForeignKey(e => e.BankAccountID)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Period)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccBankReconciliationLine>(entity =>
            {
                entity.ToTable("BankReconciliationLine", "acc");
                entity.HasKey(e => e.ReconLineID);
                entity.Property(e => e.TransactionDate).HasColumnType("date");
                entity.Property(e => e.Description).HasMaxLength(300);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Reconciliation)
                    .WithMany(r => r.Lines)
                    .HasForeignKey(e => e.ReconciliationID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.VoucherLine)
                    .WithMany()
                    .HasForeignKey(e => e.VoucherLineID)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.ChequeRegister)
                    .WithMany()
                    .HasForeignKey(e => e.ChequeRegisterID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AccAccountingAuditLog>(entity =>
            {
                entity.ToTable("AccountingAuditLog", "acc");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.LogID).ValueGeneratedOnAdd();
                entity.Property(e => e.TableName).HasMaxLength(100);
                entity.Property(e => e.RecordID).HasMaxLength(50);
                entity.Property(e => e.Action).HasMaxLength(20);
                entity.Property(e => e.OldValues);
                entity.Property(e => e.NewValues);
                entity.Property(e => e.ChangedBy).HasMaxLength(10);
                entity.Property(e => e.ChangedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IPAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(300);
            });
        }
    }
}
