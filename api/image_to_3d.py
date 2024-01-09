import uuid

from gradio_client import Client
client = Client("https://one-2-3-45-one-2-3-45.hf.space/")


def one_two_three_four_five(task_id: uuid.UUID):
    print(f"Loading image from {'F:/wish3d/' + str(task_id) + '/0.png'}")
    generated_mesh_filepath = client.predict(
        'F:/wish3d/' + str(task_id) + "/0.png",
        True,  # image preprocessing
        api_name="/generate_mesh"
    )

    return generated_mesh_filepath
