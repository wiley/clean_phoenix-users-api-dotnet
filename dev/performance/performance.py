import requests
import concurrent.futures
import time
from faker import Faker
import random
import json
import mysql.connector
import string
import traceback

# Replace these values with your actual database credentials
db_config = {
    'host': 'localhost',
    'user': 'usersapi',
    'password': 'users@pi!',
    'database': 'usersapi-qa',
    'port' : 50204
}
api_key = "DEV_API_KEY"

# List of API URLs
api_urls = [
    "http://localhost:50103/api/v4/users",
    "http://localhost:50105/api/v4/users",
    "http://localhost:50106/api/v4/users",
    "http://localhost:50107/api/v4/users",
    "http://localhost:50108/api/v4/users",
    "http://localhost:50103/api/v4/users",
    "http://localhost:50105/api/v4/users",
    "http://localhost:50106/api/v4/users",
    "http://localhost:50107/api/v4/users",
    "http://localhost:50108/api/v4/users",
]

# Split payloads into batches of 10
batch_size = len(api_urls)
users_to_create = 100


def get_random_string(length):
    # choose from all lowercase letter
    letters = string.ascii_lowercase
    return ''.join(random.choice(letters) for i in range(length))


fake = Faker()

# Function to generate a random payload
def generate_random_payload():
    return {
        "username": "smartarell." + get_random_string(20) + "@crossknowledge.com",
        "firstName": fake.first_name(),
        "lastName": fake.last_name(),
        "password": fake.password(),
    }

# Function to make a POST request to an API
def make_post_request(url, payload):
    headers = {
        "X-API-KEY": api_key,
    }    

    response = requests.post(url, json=payload, headers=headers)
    return url, response.status_code, response.text, payload

# Function to make a PUT request to an API
def make_put_request(url, payload):
    headers = {
        "X-API-KEY": api_key,
    }    

    url = url + "/" + str(payload['UserID'])
    update_payload = {
         "username": payload['username'],
        "firstName": payload['firstName'],
        "lastName": payload['lastName'],
        "city": fake.city(),
    }

    response = requests.put(url, json=update_payload, headers=headers)
    return url, response.status_code, response.text, payload

def fetch_users_from_mysql():
    # Establish a connection to the MySQL database
    connection = mysql.connector.connect(**db_config)

    try:
        # Create a cursor object to interact with the database
        cursor = connection.cursor()

        # Define your SELECT query
        query = "SELECT UserID, username, lastName, firstName FROM Users WHERE UserID IS NOT NULL ORDER BY UserID LIMIT " + str(users_to_create) 

        # Execute the query
        cursor.execute(query)

        # Fetch all the rows as a list of tuples
        result = cursor.fetchall()

        # Transform the result into a list of dictionaries
        columns = [desc[0] for desc in cursor.description]
        result_list = [dict(zip(columns, row)) for row in result]

        return result_list

    finally:
        # Close the cursor and the connection
        cursor.close()
        connection.close()


def create_users():

    # Record the start time
    total_create_start_time = time.time()

    created_users = []

    # Use ThreadPoolExecutor for parallel execution
    with concurrent.futures.ThreadPoolExecutor() as executor:
        # Generate 100 random payloads
        payloads = [generate_random_payload() for _ in range(users_to_create)]

        # Split payloads into batches 
        payload_batches = [payloads[i:i+batch_size] for i in range(0, len(payloads), batch_size)]

        for payload_batch in payload_batches:

            start_time = time.time()        
            # Submit tasks for each batch with a randomly selected URL
            future_to_url = {
                executor.submit(make_post_request, api_urls[idx], payload_batch[idx]): idx
                for idx in range(0, len(api_urls))
            }

            # Wait for the tasks to complete
            for future in concurrent.futures.as_completed(future_to_url):            
                try:
                    # Get the result of the completed task
                    result = future.result()     
                    if result[1] == 200 :  
                        jsonResult = json.loads(result[2])      
                        created_users.append(jsonResult)                                       
                    print(f"URL: {result[0]}, Status Code: {result[1]}, Response Content: {result[2]}, Payload: {result[3]}")
                except Exception as e:
                    print(f"Exception: {e}")
            end_time = time.time() 
            batch_execution_time = end_time - start_time
            print("Batch Execution Time:", batch_execution_time, "seconds")
            print("=========================================================\n")
            #time.sleep(2)

    # Record the end time
    total_create_end_time = time.time()

    # Calculate and print the total execution time
    execution_time = total_create_end_time - total_create_start_time
    print("Total Create Execution Time:", execution_time, "seconds")
    return created_users

def update_users(users):
    # Record the start time
    total_update_start_time = time.time()

    # Use ThreadPoolExecutor for parallel execution
    with concurrent.futures.ThreadPoolExecutor() as executor:

        payload_batches = [users[i:i+batch_size] for i in range(0, len(users), batch_size)]

        for payload_batch in payload_batches:

            start_time = time.time()        
            # Submit tasks for each batch with a randomly selected URL
            future_to_url = {
                executor.submit(make_put_request, api_urls[idx], payload_batch[idx]): idx
                for idx in range(0, len(payload_batch))
            }

            # Wait for the tasks to complete
            for future in concurrent.futures.as_completed(future_to_url):            
                try:
                    # Get the result of the completed task
                    result = future.result()                                                              
                    #print(f"URL: {result[0]}, Status Code: {result[1]}, Response Content: {result[2]}, Payload: {result[3]}")
                except Exception as e:
                    print(f"Exception: {e}")
                    traceback.print_exc() 
            end_time = time.time() 
            batch_execution_time = end_time - start_time
            #print("Batch Execution Time:", batch_execution_time, "seconds")
            #print("=========================================================\n")    

    # Record the end time
    total_update_end_time = time.time()

    # Calculate and print the total execution time
    execution_time = total_update_end_time - total_update_start_time
    print("Total Update Execution Time:", execution_time, "seconds")

created_users = create_users()


created_users = fetch_users_from_mysql()
print(f"MYSQL result count: {len(created_users)}")
update_users(created_users)

