variable "telegram-token" {
  type = string
}

variable "telegram-bot-url" {
  type = string
}

variable "auth-client-id" {
  type = string
}

variable "auth-client-secret" {
  type = string
}

variable "auth-callback-url" {
  type = string
}

variable "auth-scopes" {
  type = list(string)
}

variable "database-connection-string" {
  type = string
}

variable "database-name" {
  type = string
}

variable "generator-schedule" {
  type = string
}

variable "redis-connection-string" {
  type = string
}

variable "storage-queue-name" {
  type = string
}

variable "env" {
  type    = string
  default = "prd"
}