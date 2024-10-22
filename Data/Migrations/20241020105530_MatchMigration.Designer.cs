﻿// <auto-generated />
using System;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241020105530_MatchMigration")]
    partial class MatchMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Data.Entities.City", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CountryId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CountryId");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("Uq_City_Name");

                    b.ToTable("City", null, t =>
                        {
                            t.HasCheckConstraint("CK_City_Name_Length_Less_Than_64", "Length(\"Name\") <= 64");
                        });
                });

            modelBuilder.Entity("Data.Entities.Coach", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TeamId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("TeamId");

                    b.ToTable("Coach", null, t =>
                        {
                            t.HasCheckConstraint("CK_Coach_Name_Length_Less_Than_40", "Length(\"Name\") <= 40");
                        });
                });

            modelBuilder.Entity("Data.Entities.Country", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Flag")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Countries");
                });

            modelBuilder.Entity("Data.Entities.Group", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("WorldCupId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("WorldCupId");

                    b.ToTable("Group", null, t =>
                        {
                            t.HasCheckConstraint("CK_Group_Name_Length_Less_Than_10", "Length(\"Name\") <= 10");
                        });
                });

            modelBuilder.Entity("Data.Entities.Match", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("StadiumId")
                        .HasColumnType("integer");

                    b.Property<int>("TeamOneId")
                        .HasColumnType("integer");

                    b.Property<int>("TeamTwoId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("StadiumId");

                    b.HasIndex("TeamOneId");

                    b.HasIndex("TeamTwoId");

                    b.ToTable("Match", (string)null);
                });

            modelBuilder.Entity("Data.Entities.Stadium", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CityId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CityId");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("Uq_Stadium_Name");

                    b.ToTable("Stadium", null, t =>
                        {
                            t.HasCheckConstraint("CK_Stadium_Name_Length_Less_Than_64", "Length(\"Name\") <= 64");
                        });
                });

            modelBuilder.Entity("Data.Entities.Team", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CountryId")
                        .HasColumnType("integer");

                    b.Property<int>("GroupId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CountryId")
                        .IsUnique();

                    b.HasIndex("GroupId");

                    b.ToTable("Team", (string)null);
                });

            modelBuilder.Entity("Data.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Data.Entities.WorldCup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Year")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("WorldCups");
                });

            modelBuilder.Entity("Data.Entities.City", b =>
                {
                    b.HasOne("Data.Entities.Country", "Country")
                        .WithMany("Cities")
                        .HasForeignKey("CountryId")
                        .IsRequired()
                        .HasConstraintName("FK_City_Country");

                    b.Navigation("Country");
                });

            modelBuilder.Entity("Data.Entities.Coach", b =>
                {
                    b.HasOne("Data.Entities.Team", "Team")
                        .WithMany("Coach")
                        .HasForeignKey("TeamId")
                        .IsRequired()
                        .HasConstraintName("FK_Team_Coach");

                    b.Navigation("Team");
                });

            modelBuilder.Entity("Data.Entities.Group", b =>
                {
                    b.HasOne("Data.Entities.WorldCup", "WorldCup")
                        .WithMany("Groups")
                        .HasForeignKey("WorldCupId")
                        .IsRequired()
                        .HasConstraintName("FK_Worldcup_Group");

                    b.Navigation("WorldCup");
                });

            modelBuilder.Entity("Data.Entities.Match", b =>
                {
                    b.HasOne("Data.Entities.Stadium", "Stadium")
                        .WithMany("Match")
                        .HasForeignKey("StadiumId")
                        .IsRequired()
                        .HasConstraintName("FK_Stadium_Match");

                    b.HasOne("Data.Entities.Team", "TeamOne")
                        .WithMany("TeamOneMatches")
                        .HasForeignKey("TeamOneId")
                        .IsRequired()
                        .HasConstraintName("FK_TeamOne_Match");

                    b.HasOne("Data.Entities.Team", "TeamTwo")
                        .WithMany("TeamTwoMatches")
                        .HasForeignKey("TeamTwoId")
                        .IsRequired()
                        .HasConstraintName("FK_TeamTwo_Match");

                    b.Navigation("Stadium");

                    b.Navigation("TeamOne");

                    b.Navigation("TeamTwo");
                });

            modelBuilder.Entity("Data.Entities.Stadium", b =>
                {
                    b.HasOne("Data.Entities.City", "City")
                        .WithMany("Stadiums")
                        .HasForeignKey("CityId")
                        .IsRequired()
                        .HasConstraintName("FK_Stadium_City");

                    b.Navigation("City");
                });

            modelBuilder.Entity("Data.Entities.Team", b =>
                {
                    b.HasOne("Data.Entities.Country", "Country")
                        .WithOne("Team")
                        .HasForeignKey("Data.Entities.Team", "CountryId")
                        .IsRequired()
                        .HasConstraintName("FK_Country_Team");

                    b.HasOne("Data.Entities.Group", "Group")
                        .WithMany("Teams")
                        .HasForeignKey("GroupId")
                        .IsRequired()
                        .HasConstraintName("FK_Group_Team");

                    b.Navigation("Country");

                    b.Navigation("Group");
                });

            modelBuilder.Entity("Data.Entities.City", b =>
                {
                    b.Navigation("Stadiums");
                });

            modelBuilder.Entity("Data.Entities.Country", b =>
                {
                    b.Navigation("Cities");

                    b.Navigation("Team")
                        .IsRequired();
                });

            modelBuilder.Entity("Data.Entities.Group", b =>
                {
                    b.Navigation("Teams");
                });

            modelBuilder.Entity("Data.Entities.Stadium", b =>
                {
                    b.Navigation("Match");
                });

            modelBuilder.Entity("Data.Entities.Team", b =>
                {
                    b.Navigation("Coach");

                    b.Navigation("TeamOneMatches");

                    b.Navigation("TeamTwoMatches");
                });

            modelBuilder.Entity("Data.Entities.WorldCup", b =>
                {
                    b.Navigation("Groups");
                });
#pragma warning restore 612, 618
        }
    }
}
