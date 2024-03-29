﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PintAPI;

#nullable disable

namespace PintAPI.Migrations
{
    [DbContext(typeof(PintApiDb))]
    [Migration("20220525094807_ERD")]
    partial class ERD
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("PintAPI.Models.Admin", b =>
                {
                    b.Property<int>("AdminId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("AdminId");

                    b.ToTable("Admins");
                });

            modelBuilder.Entity("PintAPI.Models.CareGroup", b =>
                {
                    b.Property<int>("CareGroupId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ApiKey")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("CareGroupId");

                    b.ToTable("CareGroups");
                });

            modelBuilder.Entity("PintAPI.Models.Device", b =>
                {
                    b.Property<int>("DeviceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("CreatedByAdminId")
                        .HasColumnType("int");

                    b.Property<string>("FriendlyName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Identifier")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("DeviceId");

                    b.HasIndex("CreatedByAdminId");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("PintAPI.Models.Event", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("PatientDeviceId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("EventId");

                    b.HasIndex("PatientDeviceId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("PintAPI.Models.Log", b =>
                {
                    b.Property<int>("LogId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<uint>("Battery")
                        .HasColumnType("int unsigned");

                    b.Property<uint>("Heartbeat")
                        .HasColumnType("int unsigned");

                    b.Property<int>("PatientDeviceId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.HasKey("LogId");

                    b.HasIndex("PatientDeviceId");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("PintAPI.Models.Patient", b =>
                {
                    b.Property<int>("PatientId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("CareGroupId")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Notes")
                        .HasColumnType("longtext");

                    b.HasKey("PatientId");

                    b.HasIndex("CareGroupId");

                    b.ToTable("Patients");
                });

            modelBuilder.Entity("PintAPI.Models.PatientDevice", b =>
                {
                    b.Property<int>("PatientDeviceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("DeviceId")
                        .HasColumnType("int");

                    b.Property<int>("PatientId")
                        .HasColumnType("int");

                    b.HasKey("PatientDeviceId");

                    b.HasIndex("DeviceId");

                    b.HasIndex("PatientId");

                    b.ToTable("PatientDevices");
                });

            modelBuilder.Entity("PintAPI.Models.Device", b =>
                {
                    b.HasOne("PintAPI.Models.Admin", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedByAdminId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CreatedBy");
                });

            modelBuilder.Entity("PintAPI.Models.Event", b =>
                {
                    b.HasOne("PintAPI.Models.PatientDevice", "PatientDevice")
                        .WithMany()
                        .HasForeignKey("PatientDeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PatientDevice");
                });

            modelBuilder.Entity("PintAPI.Models.Log", b =>
                {
                    b.HasOne("PintAPI.Models.PatientDevice", "PatientDevice")
                        .WithMany()
                        .HasForeignKey("PatientDeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PatientDevice");
                });

            modelBuilder.Entity("PintAPI.Models.Patient", b =>
                {
                    b.HasOne("PintAPI.Models.CareGroup", "CareGroup")
                        .WithMany()
                        .HasForeignKey("CareGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CareGroup");
                });

            modelBuilder.Entity("PintAPI.Models.PatientDevice", b =>
                {
                    b.HasOne("PintAPI.Models.Device", "Device")
                        .WithMany()
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PintAPI.Models.Patient", "Patient")
                        .WithMany()
                        .HasForeignKey("PatientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Device");

                    b.Navigation("Patient");
                });
#pragma warning restore 612, 618
        }
    }
}
