module Bolero.Web.Util

type AsyncOp<'r> =
  | Started
  | Finished of 'r