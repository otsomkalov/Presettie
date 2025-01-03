module Telegram.Helpers

let (|CommandWithData|_|) (command: string) (input: string) =
  match input.Split(" ") with
  | [| inputCommand; data |] -> if inputCommand = command then Some(data) else None
  | _ -> None