<?php
    header('Content-Type: application/json');
    $host = "maria.eng.utah.edu";
    $dbname = "pegg";
    $username = "u1431093";
    $password = "wV8MzCJHN04lsDoVMSM7feGGq";
    
    $response = ['success' => false, 'message' => ''];
    try{
        $conn = new mysqli($host, $username, $password, $dbname);
        // Check connection
        if ($conn->connect_errno) {
            throw new Exception("Connection failed: " . $conn->connect_error);
        }

        $steamId = $conn->real_escape_string($_POST["SteamId"]);
        $steamName = $conn->real_escape_string($_POST["SteamName"]);
    
        $conn->begin_transaction();
        try{
            $findUserQuery = $conn->prepare("SELECT Id FROM Users WHERE SteamId = ?");
            $findUserQuery->bind_param("s", $steamId);
            $findUserQuery->execute();
            $userCheck = $findUserQuery->get_result();

            if (!$userCheck){
                throw new Exception("Query failed: " . $conn->error);
            }
            
            if($userCheck->num_rows > 0){
                $response['success'] = true;
                $response['message'] = "User already exists";
                $conn->commit();
                echo json_encode($response);
                exit();
            }

            $insertQuery = $conn->prepare("INSERT INTO Users (Name, SteamId) VALUES (?, ?)");
            $insertQuery->bind_param("ss", $steamName, $steamId);
            $insertQuery->execute();

            $conn->commit();
            $response['success'] = true;
            $response['message'] = "Successfully added user";
            
        } catch(Exception $e){
            $conn->rollback();
            throw $e;
        }

    } catch (Exception $e){
        $response['message'] = "Error: " . $e->getMessage();
    } finally {
        if (isset($stmt)) {
            $stmt->close();
        }
        if (isset($conn)){
            $conn->close();
        }
        echo json_encode($response);
    }
   

?>

