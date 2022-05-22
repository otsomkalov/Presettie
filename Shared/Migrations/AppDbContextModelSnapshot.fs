﻿// <auto-generated />
namespace Shared.Migrations

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Metadata
open Microsoft.EntityFrameworkCore.Migrations
open Microsoft.EntityFrameworkCore.Storage.ValueConversion
open Npgsql.EntityFrameworkCore.PostgreSQL.Metadata

type AppDbContextModelSnapshot() =
    inherit ModelSnapshot()

    override this.BuildModel(modelBuilder: ModelBuilder) =
        modelBuilder
            .HasAnnotation("ProductVersion", "6.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63) |> ignore

        modelBuilder.Entity("Shared.Data.Playlist", (fun b ->

            b.Property<int>("Id")
                .IsRequired(true)
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                |> ignore

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id")) |> ignore

            b.Property<int>("PlaylistType")
                .IsRequired(true)
                .HasColumnType("integer")
                |> ignore

            b.Property<string>("Url")
                .IsRequired(true)
                .HasColumnType("text")
                |> ignore

            b.Property<Int64>("UserId")
                .IsRequired(true)
                .HasColumnType("bigint")
                |> ignore

            b.HasKey("Id")
                |> ignore


            b.HasIndex("UserId")
                |> ignore

            b.ToTable("Playlists") |> ignore

        )) |> ignore

        modelBuilder.Entity("Shared.Data.User", (fun b ->

            b.Property<Int64>("Id")
                .IsRequired(true)
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint")
                |> ignore

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<Int64>("Id")) |> ignore

            b.Property<bool>("IncludeLikedTracks")
                .IsRequired(true)
                .HasColumnType("boolean")
                |> ignore

            b.HasKey("Id")
                |> ignore


            b.ToTable("Users") |> ignore

        )) |> ignore
        modelBuilder.Entity("Shared.Data.Playlist", (fun b ->
            b.HasOne("Shared.Data.User", "User")
                .WithMany("Playlists")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                |> ignore

        )) |> ignore
        modelBuilder.Entity("Shared.Data.User", (fun b ->

            b.Navigation("Playlists")
            |> ignore
        )) |> ignore

