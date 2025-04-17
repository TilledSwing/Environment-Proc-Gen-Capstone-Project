<?php
    header('Content-Type: application/json'); 
    $host = "maria.eng.utah.edu";
    $dbname = "pegg";
    $username = "u1431093";
    $password = "wV8MzCJHN04lsDoVMSM7feGGq";
    
    $response = [
        'success' => false,
        'message' => 'Initial state',
        'data' => []
    ];
    try{
        $conn = new mysqli($host, $username, $password, $dbname);
        // Check connection
        if ($conn->connect_errno) {
            throw new Exception("Connection failed: " . $conn->connect_error);
        }

        $insertQuery = "SELECT ID, Name FROM  Users";
        $result = $conn->query($insertQuery);

        if (!$result){
            throw new Exception("Query failed: " . $conn->error);
        }

        while ($row = $result->fetch_assoc()) {
            $response["data"][] = [
                "ID" => (string)$row["ID"],
                "Name" => $row["Name"]
                ];
        }
        $response["success"] = true;
        $response["message"] = "Data retrieved successfully";

        // Close connections
        $result->close();

    } catch (Exception $e){
        $response["message"] = $e->getMessage();
    } finally {
        if(isset($conn)) {
            $conn->close();
        }
        echo json_encode($response);
    }
   

?>

