﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Client.Migrations;

[DbContext(typeof(DatabaseContext))]
[Migration("20240902030006_AddFixesTable")]
sealed partial class AddFixesTable
{
    /// <inheritdoc />
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

        modelBuilder.Entity("Database.Client.DbEntities.FixesDbEntity", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<string>("Fixes")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("fixes");

            b.Property<string>("LastUpdated")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("last_updated");

            b.HasKey("Id");

            b.ToTable("fixes", "main");
        });

        modelBuilder.Entity("Database.Client.DbEntities.HiddenTagsDbEntity", b =>
        {
            b.Property<string>("Tag")
                .HasColumnType("TEXT")
                .HasColumnName("tag");

            b.HasKey("Tag");

            b.ToTable("hidden_tags", "main");
        });

        modelBuilder.Entity("Database.Client.DbEntities.SettingsDbEntity", b =>
        {
            b.Property<string>("Name")
                .HasColumnType("TEXT")
                .HasColumnName("name");

            b.Property<string>("Value")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("value");

            b.HasKey("Name");

            b.ToTable("settings", "main");
        });

        modelBuilder.Entity("Database.Client.DbEntities.UpvotesDbEntity", b =>
        {
            b.Property<Guid>("FixGuid")
                .ValueGeneratedOnAdd()
                .HasColumnType("TEXT")
                .HasColumnName("fix_guid");

            b.Property<bool>("IsUpvoted")
                .HasColumnType("INTEGER")
                .HasColumnName("is_upvoted");

            b.HasKey("FixGuid");

            b.ToTable("upvotes", "main");
        });
#pragma warning restore 612, 618
    }
}
