terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=4.14.0"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

locals {
  tags = {
    env  = var.env
    name = "spotify-playlist-generator"
  }
}

resource "azurerm_resource_group" "rg-spotify-playlist-generator" {
  name     = "rg-spotify-playlist-generator-${var.env}"
  location = "France Central"

  tags = local.tags
}

resource "azurerm_application_insights" "appi-spotify-playlist-generator" {
  resource_group_name = azurerm_resource_group.rg-spotify-playlist-generator.name
  location            = azurerm_resource_group.rg-spotify-playlist-generator.location

  name             = "appi-spotify-playlist-generator-${var.env}"
  application_type = "web"
}

resource "azurerm_storage_account" "st-spotify-playlist-generator" {
  resource_group_name = azurerm_resource_group.rg-spotify-playlist-generator.name
  location            = azurerm_resource_group.rg-spotify-playlist-generator.location

  name                     = "stspotifyplaylistgen${var.env}"
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "Storage"

  tags = local.tags
}

resource "azurerm_storage_queue" "stq-requests-spotify-playlist-generator" {
  storage_account_name = azurerm_storage_account.st-spotify-playlist-generator.name

  name = var.storage-queue-name
}

resource "azurerm_service_plan" "asp-spotify-playlist-generator" {
  resource_group_name = azurerm_resource_group.rg-spotify-playlist-generator.name
  location            = azurerm_resource_group.rg-spotify-playlist-generator.location

  name     = "asp-spotify-playlist-generator-${var.env}"
  os_type  = "Linux"
  sku_name = "Y1"

  tags = local.tags
}

resource "azurerm_linux_function_app" "func-spotify-playlist-generator" {
  resource_group_name = azurerm_resource_group.rg-spotify-playlist-generator.name
  location            = azurerm_resource_group.rg-spotify-playlist-generator.location

  storage_account_name       = azurerm_storage_account.st-spotify-playlist-generator.name
  storage_account_access_key = azurerm_storage_account.st-spotify-playlist-generator.primary_access_key
  service_plan_id            = azurerm_service_plan.asp-spotify-playlist-generator.id

  name = "func-spotify-playlist-generator-${var.env}"

  functions_extension_version = "~4"
  builtin_logging_enabled     = false

  site_config {
    application_insights_key = azurerm_application_insights.appi-spotify-playlist-generator.instrumentation_key
    app_scale_limit          = 10

    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = merge(
    {
      Telegram__Token            = var.telegram-token
      Telegram__BotUrl           = var.telegram-bot-url
      Spotify__ClientId          = var.spotify-client-id
      Spotify__ClientSecret      = var.spotify-client-secret
      Spotify__CallbackUrl       = var.spotify-callback-url
      Database__ConnectionString = var.database-connection-string
      Database__Name             = var.database-name
      GeneratorSchedule          = var.generator-schedule
      Redis__ConnectionString    = var.redis-connection-string
      Storage__ConnectionString  = azurerm_storage_account.st-spotify-playlist-generator.primary_connection_string
      Storage__QueueName         = azurerm_storage_queue.stq-requests-spotify-playlist-generator.name
    },
    {
      for idx, scope in var.spotify-scopes : "Spotify__Scopes__${idx}" => scope
    })

  tags = local.tags
}
