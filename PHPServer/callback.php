<?php
include "vk_api.php";
Vk_Auth::Main();

class Auth
{
    // Standalone app
    public const APP_KEY = "7599776";
    public const APP_SECURED_KEY = "Imux7zhZsb683RrpYzx7";
    public const APP_SERVICE_ACCESS_KEY = "929361c6929361c6929361c66d92e0976699293929361c6cdf0215a81843b7bbf4f6feb";
    // Group settings
    public const GROUP_TOKEN = 'ed25a9c53b9b5749ede6c690c50f166cdfbbd41f7de6adc6db141a423342c6c6fdbaefa18fab38a27716a';
    public const GROUP_CONFIRM_KEY = 'ccc5736f';

    public const VERSION_API = '5.81';
}

class Vk_Auth
{
    public static function Main()
    {
        $vk = new vk_api(Auth::GROUP_TOKEN, Auth::VERSION_API); // создание экземпляра класса работы с api, принимает токен и версию api
        $data = json_decode(file_get_contents('php://input')); //Получает и декодирует JSON пришедший из ВК
        if ($data->type == 'confirmation') { //Если vk запрашивает ключ
            exit(Auth::GROUP_CONFIRM_KEY); //Завершаем скрипт отправкой ключа
        }
        $vk->sendOK(); //Говорим vk, что мы приняли callback
        $peer_id = $data->object->peer_id;
        $text = $data->object->text;
        if($text=="/run"){
            $user_id = $data->object->from_id;
            if($user_id == 387776661) {
                $fileLocation = getenv("DOCUMENT_ROOT") . "/peer_id.txt";
                $file = fopen($fileLocation, "w+");
                fwrite($file, $peer_id);
                fclose($file);
                $vk->sendMessage($peer_id, "Started");
            }
            else {
                $vk->sendMessage($peer_id, "Error:\nThe source of the command was not identified");
            }
        }
    }
}