﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20230301153002_OverwriteTargetPlaylist")]
    partial class OverwriteTargetPlaylist
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Database.Entities.Playlist", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("Disabled")
                        .HasColumnType("boolean");

                    b.Property<int>("PlaylistType")
                        .HasColumnType("integer");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("Playlists", (string)null);

                    b.HasDiscriminator<int>("PlaylistType");
                });

            modelBuilder.Entity("Database.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Database.Entities.HistoryPlaylist", b =>
                {
                    b.HasBaseType("Database.Entities.Playlist");

                    b.HasIndex("UserId");

                    b.HasDiscriminator().HasValue(1);
                });

            modelBuilder.Entity("Database.Entities.SourcePlaylist", b =>
                {
                    b.HasBaseType("Database.Entities.Playlist");

                    b.HasIndex("UserId");

                    b.HasDiscriminator().HasValue(0);
                });

            modelBuilder.Entity("Database.Entities.TargetHistoryPlaylist", b =>
                {
                    b.HasBaseType("Database.Entities.Playlist");

                    b.HasIndex("UserId");

                    b.HasDiscriminator().HasValue(3);
                });

            modelBuilder.Entity("Database.Entities.TargetPlaylist", b =>
                {
                    b.HasBaseType("Database.Entities.Playlist");

                    b.Property<bool>("Overwrite")
                        .HasColumnType("boolean");

                    b.HasIndex("UserId");

                    b.HasDiscriminator().HasValue(2);
                });

            modelBuilder.Entity("Database.Entities.User", b =>
                {
                    b.OwnsOne("Database.Entities.Settings", "Settings", b1 =>
                        {
                            b1.Property<long>("UserId")
                                .HasColumnType("bigint");

                            b1.Property<bool?>("IncludeLikedTracks")
                                .HasColumnType("boolean");

                            b1.Property<int>("PlaylistSize")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer")
                                .HasDefaultValue(20);

                            b1.HasKey("UserId");

                            b1.ToTable("Users");

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.Navigation("Settings")
                        .IsRequired();
                });

            modelBuilder.Entity("Database.Entities.HistoryPlaylist", b =>
                {
                    b.HasOne("Database.Entities.User", "User")
                        .WithMany("HistoryPlaylists")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Database.Entities.SourcePlaylist", b =>
                {
                    b.HasOne("Database.Entities.User", "User")
                        .WithMany("SourcePlaylists")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Database.Entities.TargetHistoryPlaylist", b =>
                {
                    b.HasOne("Database.Entities.User", "User")
                        .WithMany("TargetHistoryPlaylists")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Database.Entities.TargetPlaylist", b =>
                {
                    b.HasOne("Database.Entities.User", "User")
                        .WithMany("TargetPlaylists")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Database.Entities.User", b =>
                {
                    b.Navigation("HistoryPlaylists");

                    b.Navigation("SourcePlaylists");

                    b.Navigation("TargetHistoryPlaylists");

                    b.Navigation("TargetPlaylists");
                });
#pragma warning restore 612, 618
        }
    }
}
