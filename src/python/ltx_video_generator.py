#!/usr/bin/env python3
"""
LTX Video Generator Script
Generates videos using the LTX Video model from Lightricks.
"""

import sys
import json
import os
import argparse
from pathlib import Path

def check_dependencies():
    """Check if all required dependencies are installed."""
    try:
        import torch
        import diffusers
        from diffusers import LTXPipeline, LTXImageToVideoPipeline
        from diffusers.utils import export_to_video
        return True
    except ImportError as e:
        print(f"Missing dependency: {e}", file=sys.stderr)
        print("Please install required packages: pip install -r python_requirements.txt", file=sys.stderr)
        return False

def load_request(request_path):
    """Load generation request from JSON file."""
    try:
        with open(request_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        print(f"Error loading request file: {e}", file=sys.stderr)
        return None

def generate_video(request):
    """Generate video based on the request parameters."""
    try:
        # Import here after dependency check
        import torch
        from diffusers import LTXPipeline, LTXImageToVideoPipeline
        from diffusers.utils import export_to_video
        
        model_path = request['model_path']
        prompt = request['prompt']
        output_path = request['output_path']
        duration_seconds = request['duration_seconds']
        steps = request['steps']
        guidance_scale = request['guidance_scale']
        seed = request.get('seed')
        width = request['width']
        height = request['height']
        fps = request['fps']
        input_image = request.get('input_image')  # Optional image for image-to-video
        
        print(f"Model path: {model_path}")
        print(f"Path exists: {os.path.exists(model_path)}")
        print(f"Is file: {os.path.isfile(model_path)}")
        print(f"Is directory: {os.path.isdir(model_path)}")
        
        # Determine if model_path is a file or directory
        if os.path.isfile(model_path):
            # Single file - use from_single_file
            model_file = model_path
            model_dir = os.path.dirname(model_path)
            print(f"Loading single model file: {os.path.basename(model_file)}")
            print(f"From directory: {model_dir}")
            
            # Check if it's a .safetensors file and warn about potential issues
            if model_file.endswith('.safetensors'):
                print("WARNING: You are using a single .safetensors file.")
                print("WARNING: This may be missing required components like T5EncoderModel.")
                print("WARNING: Consider downloading the complete model directory instead.")
                print("WARNING: Use 'git clone https://huggingface.co/Lightricks/LTX-Video' for the complete model.")
            
            if not os.path.exists(model_file):
                raise FileNotFoundError(f"Model file not found: {model_file}")
        elif os.path.isdir(model_path):
            # Directory - use from_pretrained
            model_dir = model_path
            model_file = None
            print(f"Loading model from directory: {model_dir}")
            
            # Check for model_index.json
            model_index_path = os.path.join(model_dir, "model_index.json")
            if not os.path.exists(model_index_path):
                print(f"ERROR: Model directory missing model_index.json: {model_dir}")
                print("ERROR: This indicates the directory is incomplete or not a valid model directory.")
                print("ERROR: Use 'git clone https://huggingface.co/Lightricks/LTX-Video' to download the complete model.")
                raise FileNotFoundError(f"Model directory missing model_index.json: {model_dir}")
            else:
                print(f"Found model_index.json - this looks like a complete model directory")
        else:
            raise FileNotFoundError(f"Model path not found: {model_path}")
        
        # Determine which pipeline to use based on whether an image is provided
        use_image_to_video = input_image is not None and input_image.strip() != ""
        pipeline_class = LTXImageToVideoPipeline if use_image_to_video else LTXPipeline
        pipeline_name = "Image-to-Video" if use_image_to_video else "Text-to-Video"
        
        print(f"Using {pipeline_name} pipeline")
        
        # Load the LTX Video pipeline
        try:
            if model_file:
                # Load from single file
                pipe = pipeline_class.from_single_file(
                    model_file, 
                    torch_dtype=torch.bfloat16
                )
            else:
                # Load from directory
                pipe = pipeline_class.from_pretrained(
                    model_dir, 
                    torch_dtype=torch.bfloat16,
                    variant="fp16" if torch.cuda.is_available() else None
                )
        except Exception as e:
            print(f"Error loading pipeline: {e}", file=sys.stderr)
            # Fallback to standard loading
            if model_file:
                pipe = pipeline_class.from_single_file(model_file)
            else:
                pipe = pipeline_class.from_pretrained(model_dir)
        
        # Move to GPU if available
        if torch.cuda.is_available():
            pipe = pipe.to('cuda')
            print(f"Using CUDA acceleration (GPU: {torch.cuda.get_device_name()})")
            print(f"Available VRAM: {torch.cuda.get_device_properties(0).total_memory // 1024**3} GB")
        else:
            print("Using CPU (this will be significantly slower)")
        
        # Set seed for reproducibility
        if seed is not None:
            torch.manual_seed(seed)
            if torch.cuda.is_available():
                torch.cuda.manual_seed(seed)
            print(f"Using seed: {seed}")
        
        print(f"Generating video with prompt: '{prompt}'")
        print(f"Parameters: {width}x{height}, {duration_seconds}s @ {fps}fps, {steps} steps")
        
        # Calculate number of frames
        num_frames = duration_seconds * fps
        print(f"Total frames to generate: {num_frames}")
        
        # Load input image if provided
        image = None
        if use_image_to_video:
            from PIL import Image
            print(f"Loading input image: {input_image}")
            if os.path.exists(input_image):
                image = Image.open(input_image).convert('RGB')
                print(f"Loaded image: {image.size}")
            else:
                raise FileNotFoundError(f"Input image not found: {input_image}")
        
        # Generate video
        try:
            if use_image_to_video:
                result = pipe(
                    image=image,
                    prompt=prompt,
                    num_inference_steps=steps,
                    guidance_scale=guidance_scale,
                    width=width,
                    height=height,
                    num_frames=num_frames
                )
            else:
                result = pipe(
                    prompt=prompt,
                    num_inference_steps=steps,
                    guidance_scale=guidance_scale,
                    width=width,
                    height=height,
                    num_frames=num_frames
                )
            
            video_frames = result.frames[0]
            
        except Exception as e:
            print(f"Error during video generation: {e}", file=sys.stderr)
            raise
        
        # Ensure output directory exists
        output_dir = os.path.dirname(output_path)
        os.makedirs(output_dir, exist_ok=True)
        
        print(f"Saving video to: {output_path}")
        
        # Export to video file
        export_to_video(video_frames, output_path, fps=fps)
        
        # Verify the file was created
        if os.path.exists(output_path):
            file_size = os.path.getsize(output_path)
            print(f"Video generation completed successfully!")
            print(f"Output file: {output_path}")
            print(f"File size: {file_size / (1024*1024):.2f} MB")
        else:
            raise FileNotFoundError("Video file was not created")
            
    except Exception as e:
        print(f"Error during video generation: {str(e)}", file=sys.stderr)
        raise

def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description='Generate videos using LTX Video model')
    parser.add_argument('request_file', nargs='?', help='JSON file containing generation request')
    parser.add_argument('--check-deps', action='store_true', help='Check dependencies and exit')
    
    args = parser.parse_args()
    
    # Check dependencies first
    if not check_dependencies():
        sys.exit(1)
    
    if args.check_deps:
        print("All dependencies are available")
        sys.exit(0)
    
    # If not checking deps, request_file is required
    if not args.request_file:
        parser.error("request_file is required when not using --check-deps")
    
    # Load request
    request = load_request(args.request_file)
    if request is None:
        sys.exit(1)
    
    try:
        generate_video(request)
        print("SUCCESS: Video generation completed")
    except KeyboardInterrupt:
        print("Generation cancelled by user", file=sys.stderr)
        sys.exit(130)
    except Exception as e:
        print(f"FAILED: {str(e)}", file=sys.stderr)
        sys.exit(1)

if __name__ == '__main__':
    main() 